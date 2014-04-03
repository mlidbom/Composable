using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.QueryModels;
using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using JetBrains.Annotations;
using NServiceBus;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    //[UsedImplicitly]
    //public class EmailToAccountMapQueryModelUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    //{
    //    private readonly IAccountManagementQueryModelUpdaterSession _documentDbModels;
    //    private readonly IAccountManagementDocumentDbReader _generatedModels;

    //    public EmailToAccountMapQueryModelUpdater(IAccountManagementQueryModelUpdaterSession documentDbModels, IAccountManagementDocumentDbReader generatedModels)
    //    {
    //        _documentDbModels = documentDbModels;
    //        _generatedModels = generatedModels;
    //    }

    //    public void Handle(IAccountEmailPropertyUpdatedEvent message)
    //    {
    //        var previousAccountVersion = _generatedModels.Get<AccountQueryModel>(message.AggregateRootId);
    //        var previousEmail = previousAccountVersion.Email;
    //        var newEmail = message.Email;

    //        if(previousEmail != null)
    //        {
    //            _documentDbModels.Delete<EmailToAccountMapQueryModel>(previousEmail);
    //        }
    //        _documentDbModels.Save(newEmail, new EmailToAccountMapQueryModel(newEmail, message.AggregateRootId));
    //    }
    //}
}
