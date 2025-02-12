﻿using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class ControlsHUDTest : TestsBase
    {
        private ControlsHUDController controller;

        protected override IEnumerator SetUp()
        {
            yield return base.SetUp();
            controller = new ControlsHUDController();
        }

        [UnityTest]
        public IEnumerator CreateView()
        {
            Assert.NotNull(controller.view);
            Assert.NotNull(controller.view.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator OpenAndCloseCorrectly()
        {
            controller.SetVisibility(true);
            Assert.IsTrue(controller.view.gameObject.activeSelf, "controls hud should be visible");
            controller.view.showHideAnimator.Hide(true);
            Assert.IsFalse(controller.view.gameObject.activeSelf, "controls hud should not be visible");
            yield break;
        }
    }
}