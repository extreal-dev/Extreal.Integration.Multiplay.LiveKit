using System;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.ConfirmationScreen
{
    public struct Confirmation
    {
        public string Message { get; private set; }
        public Action OkAction { get; private set; }
        public Action CancelAction { get; private set; }

        public Confirmation(string message, Action okAction = null, Action cancelAction = null)
        {
            Message = message;
            OkAction = okAction;
            CancelAction = cancelAction;
        }
    }
}
