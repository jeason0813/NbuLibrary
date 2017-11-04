/// <reference path="../../nbulibrary.web/scripts/client.js" />
/// <reference path="knockout-2.2.0.debug.js" />

NbuLib.Core.AccountModule = {};

NbuLib.Core.AccountModule.PendingUsersVM = function () {
    NbuLib.Core.AccountModule.PendingUsersVM.base.call(this);

    var self = this;

    //$.get('/api/Account/GetPendingRegistrations', { ts: new Date().getTime() }, function (data) {
    //    self.loadItems(data);
    //});

    //self.loadAjax("/api/Account/GetPendingRegistrations");
    self.load({ Entity: 'User', Filter: [NbuLib.QueryBuilder.makeFilter('IsActive', 'is', 'false')], SortBy: [NbuLib.QueryBuilder.makeSort('FirstName'), NbuLib.QueryBuilder.makeSort('LastName')] });

    self.Actions.push({ Label: 'Decline', Callback: self.decline, Icon: 'icon-remove' });
    self.DefaultAction = { Label: 'Approve', Callback: self.approve, Icon: 'icon-ok' };
}

NbuLib.extend(NbuLib.Core.AccountModule.PendingUsersVM, NbuLib.Core.ListVM);

NbuLib.Core.AccountModule.PendingUsersVM.prototype.ViewName = "Account_PendingRegistrations";

NbuLib.Core.AccountModule.PendingUsersVM.prototype.approve = function (user) {
    var self = this;

    $.post("/api/Account/ApproveRegistration", user, function (resp) {
        if (resp.success) {
            client.showSuccess("The registration of " + user.Email() + " was approved. The user is now active.");
            //self.Items.remove(user);
            self.reload();
        }
        else
            client.showError(resp.error)
    });
}

NbuLib.Core.AccountModule.PendingUsersVM.prototype.decline = function (user) {
    var self = this;

    $.post("/api/Account/DeclineRegistration", user, function (resp) {
        if (resp.success) {
            client.showSuccess("The registration of " + user.Email() + " was declined. An email was sent to the user to inform him that his account will be deleted.");
            //self.Items.remove(user);
            self.reload();
        }
        else
            client.showError(resp.error)
    });
}


$(function () {

    var pages = [];
    pages.push(new NbuLib.Core.Page("Pending Registrations", "Grid", NbuLib.Core.AccountModule.PendingUsersVM));

    var accountModule = new NbuLib.Core.Module(1, "Account", pages);
    client.register(accountModule);
});