using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace resinvessel.src
{
    class ResinVesselMod: ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("resinvessel", typeof(ResinVesselBlock));
            api.RegisterBlockBehaviorClass("resinvesselb", typeof(ResinVesselBehavior));
            Console.WriteLine("Start mod system");
        }
    }

    public class ResinVesselBlock: Block
    {

    }

    public class ResinVesselBehavior : BlockBehavior
    {
        // public static string NAME { get; } = "ResinVessel";

        public ResinVesselBehavior(Block block)
            : base(block) { }

        public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea)
        {
            Console.WriteLine("Can attach?");
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

            // Prefer selected block face
            if (blockSel.Face.IsHorizontal)
            {
                Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(blockSel.Face.Code));
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }

            return false;
        }


        public override bool OnBlockInteractStart(
            IWorldAccessor world, IPlayer byPlayer,
            BlockSelection blockSel, ref EnumHandling handling)
        {
            Console.WriteLine("interacted");
            return true;
        }
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handled)
        {
            Console.WriteLine("placed");
        }
    }
}
