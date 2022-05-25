﻿// <auto-generated />
using System;
using NUnit.Framework;
using Xamarin.UITest;

namespace CovidRadar.UITestV2
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public abstract class BaseTestFixture
    {
        protected IApp app => AppManager.App;
        protected bool OnAndroid => AppManager.Platform == Platform.Android;
        protected bool OniOS => AppManager.Platform == Platform.iOS;

        protected BaseTestFixture(Platform platform)
        {
            AppManager.Platform = platform;
        }

        [SetUp]
        public virtual void BeforeEachTest()
        {
            AppManager.StartApp();
            TutorialPageFlow TutorialPageFlow = new TutorialPageFlow();
            TutorialPageFlow.Tutorial();
        }


        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            
            Console.WriteLine("OneTimeSetUp");
        }

        [TearDown]
        public virtual void TearDown()
        {
            Console.WriteLine("TearDown");
       
        }

    }
}