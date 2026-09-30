using System;
using System.Collections.Generic;
using System.Drawing;

namespace GtsTest.Presenters
{
    public interface ICameraView
    {
        // ---------- View 触发的事件（Presenter 订阅） ----------
        event Action<string> ConnectClicked;   // 参数：选中的相机名称
        event Action TriggerClicked;
        event Action ContinuousToggled;
        event Action SaveImageClicked;

        // ---------- Presenter 调用的更新方法 ----------
        void SetCameraList(IEnumerable<string> cameraNames);
        void UpdateImage(Bitmap image, bool isTriggered);
        void UpdateConnectionStatus(bool connected);
        void UpdateContinuousStatus(bool isGrabbing);
        void ShowSaveDialog(Bitmap image);
        void ShowMessage(string text, string caption, MessageType type);
    }
}