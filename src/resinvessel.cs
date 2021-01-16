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
            api.RegisterBlockBehaviorClass("resinvessel", typeof(ResinVesselBehavior));
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

            return true;
        }

        public override bool OnBlockInteractStart(
            IWorldAccessor world, IPlayer byPlayer,
            BlockSelection blockSel, ref EnumHandling handling)
        {
            (world as IServerWorldAccessor).CreateExplosion(
                blockSel.Position, EnumBlastType.RockBlast, 5.0, 8.0);
            handling = EnumHandling.PreventDefault;
            return true;
        }
    }
}
