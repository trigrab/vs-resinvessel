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
using Newtonsoft.Json.Linq;
using Vintagestory.API.Server;

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
        }
    }

    public class BlockResinVessel : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }
    }

    public class BlockEntityResinVessel : BlockEntityGenericTypedContainer
    {
        public BlockPos leakingLogBlockPos;
        public JProperty leakingLogTransientProps;
        public int inGameHours { get { return (int) leakingLogTransientProps.Value["inGameHours"]; } }
        public string harvestBlockCode { get { return (string) leakingLogTransientProps.Value["convertFrom"]; } }
        public string resinBlockCode { get { return (string)leakingLogTransientProps.Value["convertTo"]; } }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTickInChunk, 1000);
            retrieveOnly = true;
        }

        public void OnTickInChunk(float par)
        {
            if (leakingLogBlockPos != null)
            {
                Block leakingLogBlock = Api.World.BlockAccessor.GetBlock(leakingLogBlockPos);
                if (CheckLeakingLogBlock(leakingLogBlock))
                {
                    BlockBehaviorHarvestable harvestableLog = GetBlockBehaviorHarvestable(leakingLogBlock);
                    HarvestResin(harvestableLog);
                    ReplaceWithHarvested(leakingLogBlockPos);
                }
            }
            else
            {
                SelectLeakingLog();
            }
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

        private void SelectLeakingLog()
        {
            foreach (int i in new int[] { -1, 1 })
            {
                int[] vectorX = { i, 0, 0 };
                int[] vectorZ = { 0, 0, i };
                foreach (int[] j in new int[][] { vectorX, vectorZ })
                {
                    BlockPos blockPos = Pos.AddCopy(j[0], j[1], j[2]);
                    Block leakingBlock = Api.World.BlockAccessor.GetBlock(blockPos);
                    if (CheckLeakingLogBlock(leakingBlock, false))
                    {
                        leakingLogBlockPos = blockPos;
                        UpdateTransientProps(blockPos);
                    }
                }
            }
        }

        public bool CheckLeakingLogBlock(Block block, bool checkLeaking = true)
        {
            if (block != null)
            {
                if (block.Code != null)
                {
                    string code = "log-resin";
                    code += (checkLeaking) ? "-" : "";
                    if (block.Code.BeginsWith("game", code))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private BlockBehaviorHarvestable GetBlockBehaviorHarvestable(Block block)
        {
            foreach (BlockBehavior blockBehavior in block.BlockBehaviors)
            {
                if (blockBehavior is BlockBehaviorHarvestable)
                {
                    return (BlockBehaviorHarvestable)blockBehavior;
                }
            }
            return null;
        }

        private void ReplaceWithHarvested(BlockPos blockPos)
        {
            Block leakingBlock = Api.World.BlockAccessor.GetBlock(blockPos);
            AssetLocation harvestedLogBlockCode = AssetLocation.Create(harvestBlockCode, leakingBlock.Code.Domain);
            Block harvestedLogBlock = Api.World.GetBlock(harvestedLogBlockCode);
            Api.World.BlockAccessor.SetBlock(harvestedLogBlock.BlockId, blockPos);
        }

        private void HarvestResin(BlockBehaviorHarvestable behavior)
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
            string codePath = (!Inventory.Empty) ? Block.Code.Path.Replace("empty", "filled") : Block.Code.Path.Replace("filled", "empty");
            AssetLocation filledBlockAsset = AssetLocation.Create(codePath, Block.Code.Domain);
            Block filledBlock = Api.World.GetBlock(filledBlockAsset);
            Api.World.BlockAccessor.ExchangeBlock(filledBlock.BlockId, Pos);
        }

        private void UpdateTransientProps(BlockPos leakingBlockPos)
        {
            Block leakingBlock = Api.World.BlockAccessor.GetBlock(leakingBlockPos);
            foreach (JProperty obj in leakingBlock.Attributes.Token)
            {
                if (obj.Name == "transientProps")
                {
                    leakingLogTransientProps = obj;
                }
            }
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
