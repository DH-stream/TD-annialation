using UnityEngine;
using UnityEngine.UIElements;

namespace TDAnnihilation
{
    public sealed class TDHudCooldownRing : VisualElement
    {
        private readonly Color trackColor;
        private readonly Color progressColor;
        private float progress;

        public TDHudCooldownRing(Color progressColor)
        {
            this.progressColor = progressColor;
            trackColor = new Color(progressColor.r, progressColor.g, progressColor.b, 0.24f);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f - 2f;
            if (radius <= 0f) return;
            Vector2 center = contentRect.center;
            Painter2D painter = context.painter2D;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            if (progress < 1f)
            {
                painter.BeginPath();
                painter.MoveTo(center);
                int steps = Mathf.Max(8, Mathf.CeilToInt((1f - progress) * 48f));
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (-90f + 360f * (1f - progress) * i / steps) * Mathf.Deg2Rad;
                    painter.LineTo(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius - 3f));
                }
                painter.ClosePath();
                painter.fillColor = new Color(2f / 255f, 4f / 255f, 8f / 255f, 0.38f);
                painter.Fill();
            }
            DrawArc(painter, center, radius, 1f, trackColor);
            if (progress > 0f)
            {
                Color lit = progressColor;
                if (progress >= 1f) lit.a = .82f + .18f * Mathf.Sin(Time.unscaledTime * 4f);
                DrawArc(painter, center, radius, progress, lit);
            }
        }

        private static void DrawArc(Painter2D painter, Vector2 center, float radius, float fraction, Color color)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt(fraction * 48f));
            painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float angle = (-90f + 360f * fraction * i / steps) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.strokeColor = color;
            painter.Stroke();
        }
    }
}
