using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Text;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;

namespace TextControlBoxNS.Core.Renderer
{
    internal class LineNumberRenderer
    {
        public CanvasTextLayout LineNumberTextLayout = null;
        public CanvasTextFormat LineNumberTextFormat = null;

        public string LineNumberTextToRender;
        public string OldLineNumberTextToRender;

        private readonly StringBuilder LineNumberContent = new StringBuilder();
        private bool needsUpdate = false;

        private TextManager textManager;
        private TextRenderer textRenderer;
        private DesignHelper designHelper;
        private LineNumberManager lineNumberManager;
        private TextLayoutManager textLayoutManager;
        private ScrollManager scrollManager;

        public void Init(
            TextManager textManager,
            TextLayoutManager textLayoutManager,
            TextRenderer textRenderer,
            DesignHelper designHelper,
            LineNumberManager lineNumberManager,
            ScrollManager scrollManager)
        {
            this.textManager = textManager;
            this.textRenderer = textRenderer;
            this.designHelper = designHelper;
            this.lineNumberManager = lineNumberManager;
            this.textLayoutManager = textLayoutManager;
            this.scrollManager = scrollManager;
        }

        public void GenerateLineNumberText(int renderedLines, int startLine)
        {
            //TODO! check performance:
            for (int i = 0; i < renderedLines; i++)
            {
                LineNumberContent.AppendLine(lineNumberManager.GetLabel(i + startLine));
            }
            LineNumberTextToRender = LineNumberContent.ToString();
            LineNumberContent.Clear();
        }

        public bool CanUpdateCanvas()
        {
            return needsUpdate || OldLineNumberTextToRender == null ||
                LineNumberTextToRender == null ||
                !OldLineNumberTextToRender.Equals(LineNumberTextToRender, StringComparison.OrdinalIgnoreCase);
        }

        public void NeedsUpdateLineNumbers()
        {
            this.needsUpdate = true;
        }

        public void HideLineNumbers(CanvasControl canvas)
        {
            canvas.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        public void Draw(CanvasControl canvas, CanvasDrawEventArgs args, float spaceBetweenCanvasAndText)
        {
            if (LineNumberTextFormat == null)
                CreateLineNumberTextFormat();

            if (LineNumberTextFormat == null)
            {
                return;
            }

            GenerateVisibleLineNumberText(canvas);
            if (LineNumberTextToRender == null || LineNumberTextToRender.Length == 0)
            {
                OldLineNumberTextToRender = LineNumberTextToRender;
                needsUpdate = false;
                return;
            }

            string widthReference = lineNumberManager.GetWidthReference(textManager.LinesCount);
            if (widthReference.Length == 0)
            {
                OldLineNumberTextToRender = LineNumberTextToRender;
                needsUpdate = false;
                return;
            }

            float lineNumberWidth = (float)Utils.MeasureTextSize(
                args.DrawingSession.Device,
                widthReference,
                LineNumberTextFormat).Width;
            canvas.Width = lineNumberWidth + 10 + spaceBetweenCanvasAndText;

            float posX = (float)canvas.Size.Width - spaceBetweenCanvasAndText;
            if (posX < 0) 
                posX = 0;

            OldLineNumberTextToRender = LineNumberTextToRender;

            LineNumberTextLayout?.Dispose();
            LineNumberTextLayout = textLayoutManager.CreateTextLayout(canvas, LineNumberTextFormat, LineNumberTextToRender, posX, (float)canvas.Size.Height);

            args.DrawingSession.DrawTextLayout(
                LineNumberTextLayout,
                10,
                textRenderer.TextVerticalOffset,
                designHelper.LineNumberColorBrush);
            needsUpdate = false;
        }

        private void GenerateVisibleLineNumberText(CanvasControl canvas)
        {
            float lineHeight = textLayoutManager.LineHeight;
            if (lineHeight <= 0 || canvas.ActualHeight <= 0 || textManager.LinesCount <= 0)
            {
                LineNumberTextToRender = "";
                return;
            }

            int startLine = Math.Min(
                (int)((scrollManager.VerticalScroll * scrollManager.DefaultVerticalScrollSensitivity) / lineHeight),
                textManager.LinesCount);
            int renderedLines = Math.Min(
                (int)(canvas.ActualHeight / lineHeight),
                textManager.LinesCount - startLine);
            GenerateLineNumberText(renderedLines, startLine);
        }

        public void CreateLineNumberTextFormat()
        {
            if (lineNumberManager._ShowLineNumbers)
            {
                LineNumberTextFormat?.Dispose();
                LineNumberTextFormat = textLayoutManager.CreateLinenumberTextFormat();
            }
        }

        public void CheckDispose()
        {
            LineNumberTextLayout?.Dispose();
            LineNumberTextFormat?.Dispose();
        }

        public void CheckGenerateLineNumberText()
        {
            if (lineNumberManager._ShowLineNumbers)
            {
                GenerateLineNumberText(textRenderer.NumberOfRenderedLines, textRenderer.NumberOfStartLine);
            }
        }
    }
}
