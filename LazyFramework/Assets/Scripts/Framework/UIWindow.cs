using System;
using FairyGUI;

/// <summary>
/// 提供 onWindowShowHide,继承自 FairyGUI的Window
/// 主要处理 EventManager的注册和注销
/// </summary>
public class UIWindow : Window
{
    protected override void OnHide()
    {
        WindowShowHide(false);
        base.OnHide();
    }

    protected override void OnShown()
    {
        WindowShowHide(true);
        base.OnShown();
    }

    private void WindowShowHide(bool isShow)
    {
        if (onWindowShowHide != null)
            onWindowShowHide(isShow);
    }

    public Action<bool> onWindowShowHide;

}

