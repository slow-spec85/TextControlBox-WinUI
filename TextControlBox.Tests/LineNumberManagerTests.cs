using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextControlBoxNS.Core;

namespace TextControlBox.Tests;

[TestClass]
public class LineNumberManagerTests
{
    [TestMethod]
    public void Labels_DefaultToSequentialNumbers()
    {
        var manager = new LineNumberManager();

        Assert.AreEqual("1", manager.GetLabel(0));
        Assert.AreEqual("12", manager.GetLabel(11));
        Assert.AreEqual("120", manager.GetWidthReference(120));
    }

    [TestMethod]
    public void CustomLabels_PreserveBlankRowsAndHideMissingRows()
    {
        var manager = new LineNumberManager();
        manager.SetCustomLabels(["10", "", "11"]);

        Assert.IsTrue(manager.HasCustomLabels);
        Assert.AreEqual("10", manager.GetLabel(0));
        Assert.AreEqual("", manager.GetLabel(1));
        Assert.AreEqual("11", manager.GetLabel(2));
        Assert.AreEqual("", manager.GetLabel(3));
        Assert.AreEqual("10", manager.GetWidthReference(500));
    }

    [TestMethod]
    public void ClearCustomLabels_RestoresSequentialNumbers()
    {
        var manager = new LineNumberManager();
        manager.SetCustomLabels([""]);

        manager.ClearCustomLabels();

        Assert.IsFalse(manager.HasCustomLabels);
        Assert.AreEqual("1", manager.GetLabel(0));
    }
}
