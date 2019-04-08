using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using CoreMotion;
using Foundation;
using UIKit;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.UI.Helper;

namespace Toggl.Daneel.Views
{
    [Register(nameof(SpiderOnARopeView))]
    public class SpiderOnARopeView : UIView
    {
        private const double height = 155.0;
        private const int chainLength = 8;
        private const double chainLinkHeight = height / chainLength;
        private const double chainWidth = 2;
        private const float spiderResistance = 0.75f;
        private const float spiderAttachmentLength = 1;
        private readonly CGColor ropeColor = Color.Main.SpiderNetColor.ToNativeColor().CGColor;

        private UIDynamicAnimator spiderAnimator;
        private UIGravityBehavior gravity;
        private CMMotionManager motionManager;
        private CMAcceleration? previousAcceleration;

        private UIImage spiderImage;
        private UIView spiderView;
        private UIView[] links;
        private CGPoint anchorPoint;

        public bool IsVisible { get; private set; }

        public SpiderOnARopeView()
        {
            init();
        }

        public SpiderOnARopeView(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            init();
        }

        private void init()
        {
            spiderImage = UIImage.FromBundle("icJustSpider");
            BackgroundColor = UIColor.Clear;
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (IsVisible && anchorPoint.X != Center.X)
            {
                Show();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            reset();
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            var ctx = UIGraphics.GetCurrentContext();
            if (ctx == null) return;

            if (links != null && IsVisible == true)
            {
                drawTheRope(ctx);
                rotateTheSpider();
            }
        }

        public void Show()
        {
            reset();
            anchorPoint = new CGPoint(Center.X, 0);

            spiderView = new UIImageView(spiderImage);
            AddSubview(spiderView);

            preparePhysics();

            IsVisible = true;
        }

        public void Hide()
        {
            reset();
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            if (spiderView == null) return;

            var spiderTouchRadius = spiderImage.Size.Width;
            var spiderTouchRadiusSq = spiderTouchRadius * spiderTouchRadius;
            foreach (UITouch touch in touches)
            {
                var position = touch.LocationInView(this);
                var dx = position.X - spiderView.Center.X;
                var dy = position.Y - spiderView.Center.Y;
                var distanceSq = dx * dx + dy * dy;
                if (distanceSq < spiderTouchRadiusSq)
                {
                    var direction = new CGVector(anchorPoint.X - spiderView.Center.X, anchorPoint.Y - spiderView.Center.Y);
                    applyForce(direction, touch.Force);
                    break;
                }
            }
        }

        private void reset()
        {
            foreach(var subview in Subviews)
            {
                subview.RemoveFromSuperview();
            }
            
            motionManager?.Dispose();
            gravity?.Dispose();
            spiderAnimator?.Dispose();
            spiderView?.Dispose();

            spiderAnimator = null;
            motionManager = null;
            gravity = null;
            spiderView = null;

            IsVisible = false;
        }

        private void preparePhysics()
        {
            spiderView.Center = new CGPoint(Center.X, -height - spiderImage.Size.Height);
            spiderView.Layer.AnchorPoint = new CGPoint(0.5, 0);

            spiderAnimator = new UIDynamicAnimator(this);

            var spider = new UIDynamicItemBehavior(spiderView);
            spider.Action = () => SetNeedsDisplay();
            spider.Resistance = spiderResistance;
            spiderAnimator.AddBehavior(spider);

            links = createRope();

            gravity = new UIGravityBehavior(links);
            spiderAnimator.AddBehavior(gravity);

            motionManager?.Dispose();
            motionManager = new CMMotionManager();
            motionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue, processAccelerometerData);
        }

        private UIView[] createRope()
        {
            var chain = new List<UIView>();
            UIView lastLink = null;

            for (int i = 0; i < chainLength; i++)
            {
                var chainLink = createChainLink(i * chainLinkHeight, lastLink);
                chain.Add(chainLink);
                lastLink = chainLink;
            }

            var spiderAttachment = new UIAttachmentBehavior(spiderView, UIOffset.Zero, lastLink, UIOffset.Zero);
            spiderAttachment.Length = spiderAttachmentLength;
            spiderAnimator.AddBehavior(spiderAttachment);

            chain.Add(spiderView);

            return chain.ToArray();
        }

        private UIView createChainLink(double y, UIView lastLink)
        {
            var chainLink = new UIView();
            chainLink.BackgroundColor = UIColor.Clear;
            chainLink.Frame = new CGRect(Center.X, -y, chainWidth, chainLinkHeight);

            AddSubview(chainLink);

            var chainDynamics = new UIDynamicItemBehavior(chainLink);
            spiderAnimator.AddBehavior(chainDynamics);

            var attachment = lastLink == null
                ? new UIAttachmentBehavior(chainLink, anchorPoint)
                : new UIAttachmentBehavior(chainLink, lastLink);
            attachment.Length = (nfloat)chainLinkHeight;
            spiderAnimator.AddBehavior(attachment);

            return chainLink;
        }

        private void drawTheRope(CGContext ctx)
        {
            var points = links.Select(links => links.Center).ToArray();
            var path = createCurvedPath(anchorPoint, points);
            ctx.SetStrokeColor(ropeColor);
            ctx.SetLineWidth((nfloat)chainWidth);
            ctx.AddPath(path);
            ctx.DrawPath(CGPathDrawingMode.Stroke);
        }

        private void rotateTheSpider()
        {
            // rotate the spider so it is perpendicular to a line
            // defined by its position and the anchor point
            var dx = spiderView.Center.X - anchorPoint.X;
            var dy = spiderView.Center.Y - anchorPoint.Y;
            var angle = (nfloat)(Math.Atan2(dy, dx) - Math.PI / 2.0);
            spiderView.Transform = CGAffineTransform.Rotate(spiderView.Transform, angle);
        }

        private CGPath createCurvedPath(CGPoint anchor, CGPoint[] points)
        {
            var path = new UIBezierPath();

            if (points.Length > 1)
            {
                var previousPoint = points[0];
                var startOfCurve = previousPoint;
                path.MoveTo(anchor);

                for (int i = 1; i < points.Length; i++)
                {
                    var endOfCurve = points[i];
                    var nextPoint = i < points.Length - 1 ? points[i + 1] : points[i];
                    var (controlPointB, controlPointC) = calculateControlPoints(previousPoint, startOfCurve, endOfCurve, nextPoint);
                    path.AddCurveToPoint(endOfCurve, controlPointB, controlPointC);

                    previousPoint = startOfCurve;
                    startOfCurve = endOfCurve;
                }
            }

            return path.CGPath;
        }

        private void processAccelerometerData(CMAccelerometerData data, NSError error)
        {
            if (spiderView == null) return;

            var ax = data.Acceleration.X;
            var ay = data.Acceleration.Y;
            var angle = -(nfloat)Math.Atan2(ay, ax);

            gravity.Angle = angle;

            if (previousAcceleration.HasValue)
            {
                var dx = (nfloat)(ax - previousAcceleration.Value.X);
                var dy = (nfloat)(ay - previousAcceleration.Value.Y);

                var direction = new CGVector(dx, dy);
                var magnitude = (nfloat)Math.Sqrt(dx * dx + dy * dy);
                if (magnitude > 0.25f)
                {
                    applyForce(direction, magnitude);
                }
            }

            previousAcceleration = data.Acceleration;
        }

        private void applyForce(CGVector direction, nfloat magnitude)
        {
            var force = new UIPushBehavior(UIPushBehaviorMode.Instantaneous, spiderView);
            force.PushDirection = direction;
            force.Magnitude = magnitude;
            spiderAnimator.AddBehavior(force);
        }

        // Catmull-Rom to Cubic Bezier conversion matrix:
        // |   0       1       0       0  |
        // | -1/6      1      1/6      0  |
        // |   0      1/6      1     -1/6 |
        // |   0       0       1       0  |
        private (CGPoint, CGPoint) calculateControlPoints(CGPoint a, CGPoint b, CGPoint c, CGPoint d)
            => (new CGPoint((-1.0 / 6.0 * a.X) + b.X + (1.0 / 6.0 * c.X), (-1.0 / 6.0 * a.Y) + b.Y + (1.0 / 6.0 * c.Y)),
                new CGPoint((1.0 / 6.0 * b.X) + c.X + (-1.0 / 6.0 * d.X), (1.0 / 6.0 * b.Y) + c.Y + (-1.0 / 6.0 * d.Y)));
    }
}
