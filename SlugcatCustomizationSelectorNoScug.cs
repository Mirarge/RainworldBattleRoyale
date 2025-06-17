using MeadowBattleRoyale;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MeadowBattleRoyale
{
public class SlugcatCustomizationSelectorNoScug : RectangularMenuObject
    {
        public SlugcatCustomization customization;
        public OpTinyColorPicker eyeColorSelector;
        public OpTinyColorPicker bodyColorSelector;
        public OpComboBox2 slugcatSelector;
        private OpTextBox nicknameBox;

        public SlugcatCustomizationSelectorNoScug(SmartMenu menu, MenuObject owner, Vector2 pos, SlugcatCustomization customization) : base(menu, owner, pos, new Vector2(40, 120))
        {
            this.customization = customization;

            this.subObjects.Add(new MenuLabel(menu, this, "Eye color", Vector2.zero, new Vector2(100, 18), false));
            this.eyeColorSelector = new OpTinyColorPicker(menu, pos + new Vector2(100, 0), customization.eyeColor);
            this.subObjects.Add(new MenuLabel(menu, this, "Body color", new Vector2(0, 30), new Vector2(100, 18), false));
            this.bodyColorSelector = new OpTinyColorPicker(menu, pos + new Vector2(100, 30), customization.bodyColor);
            new UIelementWrapper(menu.tabWrapper, bodyColorSelector);
            new UIelementWrapper(menu.tabWrapper, eyeColorSelector);
            
            this.nicknameBox = new OpTextBox(new Configurable<string>(customization.nickname), pos + new Vector2(140, 32), 160);
            new UIelementWrapper(menu.tabWrapper, nicknameBox);

            bodyColorSelector.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;
            eyeColorSelector.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;

            nicknameBox.OnValueChanged += NicknameBox_OnValueChanged;
            nicknameBox.OnValueUpdate += NicknameBox_OnValueUpdate;
        }

        private void NicknameBox_OnValueUpdate(UIconfig config, string value, string oldValue)
        {
            customization.nickname = value;
        }

        private void NicknameBox_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            customization.nickname = value;
        }

        private void ColorSelector_OnValueChangedEvent()
        {
            customization.eyeColor = Extensions.SafeColorRange(eyeColorSelector.valuecolor);
            customization.bodyColor = Extensions.SafeColorRange(bodyColorSelector.valuecolor);
        }
    }

 }