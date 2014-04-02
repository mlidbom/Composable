﻿using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Updaters.Tests.AccountQueryModelTests
{
    public class AfterAccountIsRegistered_Generators : AfterAccountIsRegistered
    {
        [SetUp]
        public void RewireForGenerators()
        {
            QueryModelTestWiringHelper.ReplaceDocumentDbQuerymodelsReaderWithAutoGeneratedQueryModelsReader(Container);
        }
    }
}