using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace dryingboxmod101
{
    public class dryingboxmod101ModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("dryingboxmod101Class", typeof(dryingboxmod101Class));
            api.RegisterBlockEntityClass("dryingboxmod101Entity", typeof(dryingboxmod101Entity));
        }
    }

    public class dryingboxmod101Class : BlockContainer
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as dryingboxmod101Entity;
            if (blockEntity != null)
            {
                return blockEntity.OnPlayerRightClick(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

    public class dryingboxmod101Entity : BlockEntityOpenableContainer
    {

        private GuiDialogDryingBox clientDialog;

        public dryingboxmod101Entity()
        {
            Inventory = new InventoryGeneric(2, "dryingbox-1", null, null);
        }

        public override InventoryBase Inventory { get; }

        public override string InventoryClassName => "dryingbox";

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1)
                return false;

            if (this.Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () =>
                {
                    this.clientDialog = new GuiDialogDryingBox(Lang.Get("dryingboxmod101:block-dryingboxblock"), this.Inventory, this.Pos, this.Api as ICoreClientAPI, this);
                    return clientDialog;
                });
            }
            return true;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Inventory.LateInitialize("dryingbox-1", api);
            api.Event.RegisterGameTickListener(OnGameTick, 200);
        }

        private void OnGameTick(float dt)
        {
            ProcessDrying(dt);
        }

        private double dryingTimer1 = 0.0;                  // Таймер для отслеживания времени сушки
        private float dryingTimeMax = 5;
        bool ItemHasDry = false;

        private void ProcessDrying(float dt)
        {
            var inputSlot = Inventory[0];
            var outputSlot = Inventory[1];

            if (inputSlot.Itemstack != null && outputSlot.Itemstack == null)
            {

                if (inputSlot.Itemstack.Collectible.TransitionableProps != null)
                {
                    IWorldAccessor world = Api.World;
                    var ItemHasProperties = inputSlot.Itemstack.Collectible.GetTransitionableProperties( world, inputSlot.Itemstack, null);
                    foreach (var properties in ItemHasProperties)
                    {
                        //Api.World.Logger.Notification($"Type={properties.Type}");
                        if (properties.Type.ToString() == "Dry")
                        {
                            ItemHasDry = true;
                        }
                    }
                }
                if (ItemHasDry)
                {
                    dryingTimer1 += dt;

                    if (dryingTimer1 >= dryingTimeMax)      // 5 секунд
                    {
                        // Устанавливаем новое состояние
                        var temp = inputSlot.Itemstack.Clone();
                        temp.Collectible.SetTransitionState(temp, EnumTransitionType.Dry, 10000);


                        inputSlot.Itemstack = null;         // Убираем предмет из входного слота
                        outputSlot.Itemstack = temp;        // Ставим новый предмет в выходной слот
                        outputSlot.MarkDirty();
                        dryingTimer1 = 0.0;                 // Сброс таймера
                        ItemHasDry = false;      

                    }
                }
            }
            else
            {
                dryingTimer1 = 0.0;                         // Сброс таймера
            }

            // Обновление с прогрессом сушки

            if (clientDialog != null)
            {
                clientDialog.Update((float)GetDryingProgress(), dryingTimeMax - 4);
            }
        }

        public double GetDryingProgress()
        {
            return dryingTimer1 / 5.0; 
        }
    }
    //___
    public class GuiDialogDryingBox : GuiDialogBlockEntityQuern
    {
        private dryingboxmod101Entity entity;
        public GuiDialogDryingBox(string title, InventoryBase inventory, BlockPos pos, ICoreClientAPI capi, dryingboxmod101Entity entity)
            : base(title, inventory, pos, capi)
        {
            this.entity = entity;
        }

    }
    public interface IHasDryingState 
    {
        ItemStack GetDriedItem();
    }

}