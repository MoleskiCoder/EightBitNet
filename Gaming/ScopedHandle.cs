namespace Gaming
{
    using System;
    using System.Runtime.InteropServices;

    public class ScopedHandle(IntPtr invalidHandleValue, Action<IntPtr> deleter) : SafeHandle(invalidHandleValue, true)
    {
        private readonly Action<IntPtr> _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public IntPtr Handle
        {
            get => this.handle;
            set => this.handle = value;
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
                this._deleter(this.handle);
            return true;
        }

        public static implicit operator IntPtr(ScopedHandle h) => h.DangerousGetHandle();
    }
}
