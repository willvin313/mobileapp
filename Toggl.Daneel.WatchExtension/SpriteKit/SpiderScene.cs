using System;
using CoreGraphics;
using SpriteKit;
using UIKit;

namespace Toggl.Daneel.WatchExtension.SpriteKit
{
    public sealed class SpiderScene : SKScene
    {
        public SpiderScene()
        {
            var spiderNode = SKSpriteNode.FromImageNamed("SpiderBro");
            spiderNode.XScale = (nfloat)0.004;
            spiderNode.YScale = (nfloat)0.004;
            spiderNode.AnchorPoint = new CGPoint(0.5, 1.0);
            spiderNode.Position = new CGPoint(Frame.GetMidX(), Frame.GetMaxY());

            AddChild(spiderNode);

            var angle = 30.0 * Math.PI / 180;
            var duration = 2.0;

            spiderNode.ZRotation = (nfloat)(angle / 2);

            var rightToLeft = SKAction.RotateByAngle((nfloat)(-angle), duration);
            rightToLeft.TimingMode = SKActionTimingMode.EaseInEaseOut;
            var leftToRight = SKAction.RotateByAngle((nfloat)angle, duration);
            leftToRight.TimingMode = SKActionTimingMode.EaseInEaseOut;

            var sequence = SKAction.Sequence(rightToLeft, leftToRight);

            var repeating = SKAction.RepeatActionForever(sequence);

            spiderNode.RunAction(repeating);
        }
    }
}
