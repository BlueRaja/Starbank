using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StarHack_II;

namespace StarHack_II_Tests
{
    [TestClass]
    public class BankReaderTests
    {
        private BankReader _target;

        [TestInitialize]
        public void Initialize()
        {
            _target = new BankReader();
        }

        [TestMethod]
        public void Sha1HashTests()
        {
            Assert.AreEqual(_target.Sha1Hash("The quick brown fox jumps over the lazy dog"), "2FD4E1C67A2D28FCED849EE1BB76E7391B93EB12");
            Assert.AreEqual(_target.Sha1Hash("The quick brown fox jumps over the lazy cog"), "DE9F2C7FD25E1B3AFAD3E85A0BD17D9B100DB4B3");
            Assert.AreEqual(_target.Sha1Hash(""), "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        //TODO: Test GetSignature()
    }
}
