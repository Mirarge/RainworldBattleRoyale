using HUD;
using UnityEngine;

public class PlayerWinDisplay : HUD.HudPart
{
    FLabel CenterText;
    public Vector2 pos;
    public PlayerWinDisplay(string playerWhoWon, FContainer fContainer, HUD.HUD hud) : base(hud)
    {
        string winString = playerWhoWon.ToUpper() + " WON THE GAME!";
        CenterText = new FLabel("font", winString)
        {
            scale = 2.4f,
            alignment = FLabelAlignment.Center
        };
        pos = new Vector2(hud.rainWorld.options.ScreenSize.x / 2, hud.rainWorld.options.ScreenSize.y / 2);
        CenterText.SetPosition(pos);
        fContainer.AddChild(CenterText);
    }
    
    public override void ClearSprites()
        {
            base.ClearSprites();
            CenterText.RemoveFromContainer();
        }
}