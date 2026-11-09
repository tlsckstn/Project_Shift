using NUnit.Framework;
using ProjectShift.Menu;

namespace ProjectShift.Tests
{
    public class PauseMenuTests
    {
        [Test]
        public void Open_AfterMoving_StartsOnBgm()
        {
            PauseMenu menu = new PauseMenu();
            menu.Open();
            menu.Move(2);
            menu.Close();

            menu.Open();

            Assert.AreEqual(PauseItem.Bgm, menu.Selected);
            Assert.IsTrue(menu.IsOpen);
        }

        [Test]
        public void Move_PastEnds_DoesNotWrap()
        {
            PauseMenu menu = new PauseMenu();
            menu.Open();

            menu.Move(-1);
            Assert.AreEqual(PauseItem.Bgm, menu.Selected);

            menu.Move(10);
            Assert.AreEqual(PauseItem.Stages, menu.Selected);
        }

        [Test]
        public void Close_Open_IsOpenFalse()
        {
            PauseMenu menu = new PauseMenu();
            menu.Open();

            menu.Close();

            Assert.IsFalse(menu.IsOpen);
        }
    }
}
