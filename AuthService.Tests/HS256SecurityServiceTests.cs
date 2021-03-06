﻿using System;
using AuthService.Common.Enums;
using AuthService.Common.Settings;
using AuthService.Logic.Authentification;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuthService.Tests
{
    [TestClass]
    public class HS256SecurityServiceTests
    {
        [TestMethod]
        public void GetSecurityKey_Success()
        {
            var settings = new SecuritySettings
            {
                SecurityType = SecurityTypeEnum.HS256,
                Audience = "local.auth.audience",
                Issuer = "local.auth.issuer",
                SigningKey = "***"
            };

            var mockSettings = new Mock<IOptions<SecuritySettings>>();
            mockSettings.Setup(m => m.Value).Returns(() => settings);
            var service = new Hs256SecurityService(mockSettings.Object);
            var result = service.GetSecurityKey();

            Assert.IsTrue(result != null);
        }

        [TestMethod]
        public void GetSecurityKey_SigningKeyIsEmpty_Success()
        {
            var settings = new SecuritySettings
            {
                SecurityType = SecurityTypeEnum.HS256,
                Audience = "local.auth.audience",
                Issuer = "local.auth.issuer",
            };

            var mockSettings = new Mock<IOptions<SecuritySettings>>();
            mockSettings.Setup(m => m.Value).Returns(() => settings);
            var service = new Hs256SecurityService(mockSettings.Object);

            try
            {
                var result = service.GetSecurityKey();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Setting SigningKey is null or empty"));
            }
        }
    }
}
