using System;
using System.Text;

namespace Mono.VisualStudio.TextTemplating.VHost
{
	public class TextTemplatingCallback
		: ITextTemplatingCallback
	{
		bool isFromOutputDirective;

		public bool Errors { get; set; }

		public string Extension { get; private set; } = null;

		public Encoding OutputEncoding { get; private set; } = Encoding.UTF8;

		public void ErrorCallback (bool warning, string message, int line, int column)
		{
			Errors = true;
		}

		public void SetFileExtension (string extension)
		{
			Extension = extension ?? throw new ArgumentNullException (nameof (extension));
		}

		public void SetOutputEncoding (Encoding encoding, bool fromOutputDirective)
		{
			if (!isFromOutputDirective) {
				if (fromOutputDirective) {
					isFromOutputDirective = true;
					OutputEncoding = encoding ?? throw new ArgumentNullException (nameof (encoding));
				} else {
					if ((OutputEncoding != null) && !ReferenceEquals (encoding, OutputEncoding)) {
						OutputEncoding = Encoding.UTF8;
					}
					OutputEncoding = encoding;
				}
			}
		}

		public ITextTemplatingCallback DeepCopy ()
		{
			TextTemplatingCallback callback = (TextTemplatingCallback)base.MemberwiseClone ();

			if (Extension != null) {
				callback.Extension = (string)Extension.Clone ();
			}
			if (OutputEncoding != null) {
				callback.OutputEncoding = (Encoding)OutputEncoding.Clone ();
			}

			return callback;
		}
	}
}
