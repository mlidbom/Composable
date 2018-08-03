using System;
using AccountManagement.API;
using FluentAssertions;
using FluentAssertions.Specialized;

namespace AccountManagement.UserStories.Scenarios
{
    public abstract class ScenarioBase<TResult>
    {
        protected AccountApi Api => AccountApi.Instance;

        public abstract TResult Execute();


        public ExceptionAssertions<TException> ExecutingShouldThrow<TException>() where TException : Exception => this.Invoking(@this => @this.Execute()).Should().Throw<TException>();
    }
}
