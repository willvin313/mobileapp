using System;
using System.Collections.Generic;
using CoreGraphics;
using SpriteKit;
using UIKit;

namespace Toggl.Daneel.WatchExtension.SpriteKit
{
    public sealed class Rope : SKNode
    {
        public Rope(CGPoint anchor, int partsCount, SKNode attachment)
        {
            var parts = new List<SKNode>();

            // Build rope
            var rect = new CGRect(0, 0, 2, 2);
            var firstPart = SKShapeNode.FromRect(rect);
            firstPart.FillColor = UIColor.Black;
            firstPart.Position = anchor;
            firstPart.PhysicsBody = SKPhysicsBody.CreateCircularBody(firstPart.Frame.Size.Width);
            firstPart.PhysicsBody.AllowsRotation = true;

            parts.Add(firstPart);
            Scene.Add(firstPart);

            for (var i = 1; i < partsCount; i++)
            {
                var part = SKShapeNode.FromRect(rect);
                firstPart.FillColor = UIColor.Black;
                part.Position = new CGPoint(firstPart.Position.X, firstPart.Position.Y - (i * part.Frame.Size.Height));
                part.PhysicsBody = SKPhysicsBody.CreateCircularBody(part.Frame.Size.Width);
                part.PhysicsBody.AllowsRotation = true;

                parts.Add(part);
                Scene.Add(part);
            }

            var lastPart = parts[partsCount - 1];
            attachment.Position = new CGPoint(lastPart.Position.X, lastPart.Frame.GetMaxY());
            attachment.PhysicsBody = SKPhysicsBody.CreateCircularBody(attachment.Frame.Size.Height / 2);

            parts.Add(attachment);
            Scene.Add(attachment);

            // Physics

            for (var i = 1; i < parts.Count; i++)
            {
                var nodeA = parts[i - 1];
                var nodeB = parts[i];

                SKPhysicsJointPin joint = SKPhysicsJointPin.Create(nodeA, nodeB, new CGPoint(nodeA.Frame.GetMidX(), nodeA.Frame.GetMidY()));
                Scene.PhysicsWorld.AddJoint(joint);
            }
        }
    }
}
