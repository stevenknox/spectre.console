using System;
using System.Linq;
using Spectre.Console.Internal;
using Spectre.Console.Rendering;

namespace Spectre.Console
{
    /// <summary>
    /// A column showing a spinner.
    /// </summary>
    public sealed class SpinnerColumn : ProgressColumn
    {
        private const string ACCUMULATED = "SPINNER_ACCUMULATED";
        private const string INDEX = "SPINNER_INDEX";

        private readonly object _lock;
        private Spinner _spinner;
        private int? _maxWidth;
        private string? _completed;

        /// <inheritdoc/>
        protected internal override bool NoWrap => true;

        /// <summary>
        /// Gets or sets the <see cref="Console.Spinner"/>.
        /// </summary>
        public Spinner Spinner
        {
            get => _spinner;
            set
            {
                lock (_lock)
                {
                    _spinner = value ?? Spinner.Known.Default;
                    _maxWidth = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the text that should be shown instead
        /// of the spinner once a task completes.
        /// </summary>
        public string? CompletedText
        {
            get => _completed;
            set
            {
                _completed = value;
                _maxWidth = null;
            }
        }

        /// <summary>
        /// Gets or sets the completed style.
        /// </summary>
        public Style? CompletedStyle { get; set; }

        /// <summary>
        /// Gets or sets the style of the spinner.
        /// </summary>
        public Style? Style { get; set; } = new Style(foreground: Color.Yellow);

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinnerColumn"/> class.
        /// </summary>
        public SpinnerColumn()
            : this(Spinner.Known.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinnerColumn"/> class.
        /// </summary>
        /// <param name="spinner">The spinner to use.</param>
        public SpinnerColumn(Spinner spinner)
        {
            _spinner = spinner ?? throw new ArgumentNullException(nameof(spinner));
            _lock = new object();
        }

        /// <inheritdoc/>
        public override IRenderable Render(RenderContext context, ProgressTask task, TimeSpan deltaTime)
        {
            var useAscii = (context.LegacyConsole || !context.Unicode) && _spinner.IsUnicode;
            var spinner = useAscii ? Spinner.Known.Ascii : _spinner ?? Spinner.Known.Default;

            if (!task.IsStarted || task.IsFinished)
            {
                return new Markup(CompletedText ?? " ", CompletedStyle ?? Style.Plain);
            }

            var accumulated = task.State.Update<double>(ACCUMULATED, acc => acc + deltaTime.TotalMilliseconds);
            if (accumulated >= spinner.Interval.TotalMilliseconds)
            {
                task.State.Update<double>(ACCUMULATED, _ => 0);
                task.State.Update<int>(INDEX, index => index + 1);
            }

            var index = task.State.Get<int>(INDEX);
            var frame = spinner.Frames[index % spinner.Frames.Count];
            return new Markup(frame.EscapeMarkup(), Style ?? Style.Plain);
        }

        /// <inheritdoc/>
        public override int? GetColumnWidth(RenderContext context)
        {
            return GetMaxWidth(context);
        }

        private int GetMaxWidth(RenderContext context)
        {
            lock (_lock)
            {
                if (_maxWidth == null)
                {
                    var useAscii = (context.LegacyConsole || !context.Unicode) && _spinner.IsUnicode;
                    var spinner = useAscii ? Spinner.Known.Ascii : _spinner ?? Spinner.Known.Default;

                    _maxWidth = Math.Max(
                        ((IRenderable)new Markup(CompletedText ?? " ")).Measure(context, int.MaxValue).Max,
                        spinner.Frames.Max(frame => Cell.GetCellLength(context, frame)));
                }

                return _maxWidth.Value;
            }
        }
    }
}
