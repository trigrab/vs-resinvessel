using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace resinvessel.src
{
    class ResinVesselMod: ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("resinvessel", typeof(BlockResinVessel));
            api.RegisterBlockBehaviorClass("resinvesselb", typeof(BlockBehaviorResinVessel));
            api.RegisterBlockEntityClass("resinvessel", typeof(BlockEntityResinVessel));
            Console.WriteLine("Start mod system");
        }
    }

    public class BlockResinVessel : Block
    {

    }

    public class BlockEntityResinVessel : BlockEntityGenericTypedContainer
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, 1000);
            retrieveOnly = true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            int stacksize = (Inventory.Empty) ? 0 : Inventory[0].Itemstack.StackSize;
            {

            }

            dsc.Clear();

            dsc.AppendLine(Lang.Get("Resin stored: {0}", stacksize));
        }

        public void OnTick(float par)
        {
            foreach (int i in new int[] { -1, 1 })
            {
                int[] vectorX = { i, 0, 0 };
                int[] vectorZ = { 0, 0, i };
                foreach (int[] j in new int[][] { vectorX, vectorZ })
                {
                    BlockPos blockPos = Pos.AddCopy(j[0], j[1], j[2]);
                    Block leakingPineLogBlock = ConvertBlockToLeakingPineBlockBlock(Api.World.BlockAccessor.GetBlock(blockPos));
                    if (leakingPineLogBlock != null)
                    {
                        BlockBehaviorHarvestable harvestablePineLog = GetBlockBehaviorHarvestable(leakingPineLogBlock);
                        HarvestResin(leakingPineLogBlock, harvestablePineLog);
                        ReplaceWithHarvested(blockPos);
                    }
                }
            }
        }

        private BlockBehaviorHarvestable GetBlockBehaviorHarvestable(Block block)
        {
            foreach (BlockBehavior blockBehavior in block.BlockBehaviors)
            {
                if (blockBehavior is BlockBehaviorHarvestable)
                {
                    return (BlockBehaviorHarvestable) blockBehavior;
                }
            }
            return null;
        }

        private void ReplaceWithHarvested(BlockPos blockPos)
        {
            AssetLocation harvestedPineLogBlockCode = AssetLocation.Create("log-resinharvested-pine-ud");
            Block harvestedPineLogBlock = Api.World.GetBlock(harvestedPineLogBlockCode);
            Api.World.BlockAccessor.SetBlock(harvestedPineLogBlock.BlockId, blockPos);
        }

        private void HarvestResin(Block leakingPineLog, BlockBehaviorHarvestable behavior)
        {
            ItemStack resinVesselstack = Inventory[0].Itemstack;

            float dropRate = 1;  // normally multiplied with player harvestrate

            ItemStack resinLogStack = behavior.harvestedStack.GetNextItemStack(dropRate);
            if (resinVesselstack != null)
            {
                if (resinVesselstack.Item.Code.Path == behavior.harvestedStack.Code.Path)
                {
                    resinVesselstack.StackSize += resinLogStack.StackSize;
                }
            }
            else
            {
                Inventory[0].Itemstack = resinLogStack;
            }
            UpdateAsset();
        }

        public void UpdateAsset()
        {
            String codePath = (!Inventory.Empty) ? Block.Code.Path.Replace("empty", "filled") : Block.Code.Path.Replace("filled", "empty");
            AssetLocation filledBlockAsset = AssetLocation.Create(codePath, Block.Code.Domain);
            Block filledBlock = Api.World.GetBlock(filledBlockAsset);
            Api.World.BlockAccessor.ExchangeBlock(filledBlock.BlockId, Pos);
        }

        private Block ConvertBlockToLeakingPineBlockBlock(Block block)
        {
            if (block != null)
            {
                if (block.Code != null)
                {
                    if (block.Code.BeginsWith("game", "log-resin-"))
                    {
                        return block;
                    }
                }
            }
            return null;
        }
    }

    public class BlockBehaviorResinVessel : BlockBehavior
    {
        // public static string NAME { get; } = "ResinVessel";


        public BlockBehaviorResinVessel(Block block)
            : base(block) {


        }

        public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea)
        {
            if (BlockFacing.VERTICALS.Contains(blockFace))
            {
                handling = EnumHandling.PreventDefault;
                return true;
            }
            handling = EnumHandling.PassThrough;
            return false;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            handling = EnumHandling.PreventDefault;

            BlockPos placePos = blockSel.Position.Copy();
            Block placeOn =
                world.BlockAccessor.GetBlock(placePos.Add(blockSel.Face.Opposite));


            // world.Logger.Chat("Log domain:" + placeOn.Code.Domain);
            // world.Logger.Chat("Log domain:" + placeOn.Code.BeginsWith("game", "log-resin"));

            // Prefer selected block face
            if (blockSel.Face.IsHorizontal && placeOn.Code.BeginsWith("game", "log-resin"))

            {
                foreach (BlockFacing face in BlockFacing.HORIZONTALS)
                {
                    BlockPos testPos = placePos.AddCopy(face);
                    if (IsResinVesel(world.BlockAccessor.GetBlock(testPos)))
                    {
                        /*
                        List<BlockPos> tmpList = new List<BlockPos>();
                        tmpList.Add(testPos);
                        world.HighlightBlocks(byPlayer, 2, tmpList);
                        */
                        return false;
                    }
                }
 

                Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(blockSel.Face.Code));
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                //block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

                return true;
            }

            return false;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventSubsequent;

            BlockEntity entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (entity is BlockEntityResinVessel)
            {
                BlockEntityResinVessel vessel = (BlockEntityResinVessel)entity;

                if (!vessel.Inventory.Empty)
                {
                    ItemStack stack = vessel.Inventory[0].TakeOutWhole();
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                    vessel.UpdateAsset();
                    return true;
                }
                else {
                    return false;
                }
                
            }

            return false;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;

            BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);

            if (entity is BlockEntityResinVessel)
            {
                BlockEntityResinVessel vessel = (BlockEntityResinVessel)entity;

                IPlayer[] players = world.AllOnlinePlayers;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].InventoryManager.HasInventory(vessel.Inventory))
                    {
                        players[i].InventoryManager.CloseInventory(vessel.Inventory);
                    }
                }
            }
        }

        private bool IsResinVesel(Block block)
        {
            return block.Code.BeginsWith("resinvessel", "resinvessel");
        }
    }
}
