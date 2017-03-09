#region usings

using System;
using System.Collections.Generic;
using Composable.DDD;
using NUnit.Framework;

#endregion

namespace Composable.Tests.DDD
{
    [TestFixture]
    public class ValueObjectsWithCollectionsTests
    {
        public class ExternalJobsDashboardViewModel : ValueObject<ExternalJobsDashboardViewModel>
        {
            public ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<JobAdvertisement>();
            }

            public IEnumerable<JobAdvertisement> JobAdvertisements { get; set; }
            public string CompanyState { get; set; }

            public class JobAdvertisement : ValueObject<JobAdvertisement>
            {
                public string Name { get; set; }
            }
        }

        [Test]
        public void DifferingPropertiesShouldReturnFalse()
        {
            var lhs = new ExternalJobsDashboardViewModel()
                      {
                          CompanyState = "SomeState"
                      };
            var rhs = new ExternalJobsDashboardViewModel()
                      {
                          CompanyState = "SomeOtherState"
                      };

            Assert.That(lhs, Is.Not.EqualTo(rhs));
        }

        [Test]
        public void DifferingCollectionsShouldNotBeEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                    }
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
            };

            Assert.That(lhs, Is.Not.EqualTo(rhs));
        }

        [Test]
        public void IdenticalCollectionsShouldBeEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                    }
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                    }
            };

            Assert.That(lhs, Is.EqualTo(rhs));
        }

        [Test]
        public void PropertiesDifferingInCollectionShouldNotBeEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                        {
                                            Name = "AValue"
                                        }
                                    }
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                        {
                                            Name = "ANotherValue"
                                        }
                                    }
            };

            Assert.That(lhs, Is.Not.EqualTo(rhs));
        }

        [Test]
        public void IdenticalInCollectionShouldBeEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                        {
                                            Name = "AValue"
                                        }
                                    }
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>()
                                    {
                                        new ExternalJobsDashboardViewModel.JobAdvertisement()
                                        {
                                            Name = "AValue"
                                        }
                                    }
            };

            Assert.That(lhs, Is.EqualTo(rhs));
        }

        [Test]
        public void IfOneCollectionIsNullObjectsAreNotEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = null
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>(){}
            };

            Assert.That(lhs, Is.Not.EqualTo(rhs));

            lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = new List<ExternalJobsDashboardViewModel.JobAdvertisement>() { }
            };
            rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = null
            };
            Assert.That(lhs, Is.Not.EqualTo(rhs));
        }

        [Test]
        public void IfBothCollectionsAreNullObjectsAreEqual()
        {
            var lhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = null
            };
            var rhs = new ExternalJobsDashboardViewModel()
            {
                JobAdvertisements = null
            };

            Assert.That(lhs, Is.EqualTo(rhs));
        }
    }
}