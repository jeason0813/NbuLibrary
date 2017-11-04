/// <reference path="../../nbulibrary.web/scripts/client.js" />
/// <reference path="knockout-2.2.0.debug.js" />


NbuLib.Core.AccountModule = {};

NbuLib.Core.AccountModule.UsersVM = function (group) {

    NbuLib.Core.AccountModule.UsersVM.base.call(this);

    var self = this;
    var initQuery = { Group: group || {}, SortBy: [], FilterBy: [] };

    //$.post("/api/account/ListUsers", initQuery, function (response) {
    //    self.loadItems(response);
    //});

    //self.loadAjax("/api/account/ListUsers", initQuery);
    self.load({ Entity: 'User', SortBy: [NbuLib.QueryBuilder.makeSort('FirstName'), NbuLib.QueryBuilder.makeSort('LastName')] });

    //self.Actions.push({ Label: 'Details', Callback: self.open, Icon: 'icon-folder-open' });
    self.DefaultAction = { Label: 'Details', Callback: self.open, Icon: 'icon-folder-open' };
    self.Actions.push({ Label: 'Edit', Callback: self.edit, Icon: 'icon-pencil' });
    self.Actions.push({ Label: 'Change Password', Callback: self.changePwd });
    self.Actions.push({ Label: 'Delete', Callback: self.remove, Icon: 'icon-trash' });


    self.GlobalActions.push({ Label: 'Create User', Callback: self.create, Icon: 'icon-user' });

    //self.sortByField = ko.observable();
    //self.sortByDesc = ko.observable(false);
    //self.sortBy = function (field) {
    //    var sortBy = [];
    //    if (self.sortByField() == field)
    //        self.sortByDesc(!self.sortByDesc());
    //    else {
    //        self.sortByField(field);
    //        self.sortByDesc(false);
    //    }

    //    sortBy.push({ Property: field.Property, Descending: self.sortByDesc(), Role: field.Role });
    //    var entityQuery = { Group: { Name: 'Students' }, SortBy: sortBy, FilterBy: [] };
    //    self.clearItems();
    //    self.loadAjax("/api/account/ListUsers", entityQuery);
    //    //$.post("/api/account/ListUsers", entityQuery, function (response) {
    //    //    self.Items.removeAll();
    //    //    $(response).each(function () {
    //    //        var item = this;
    //    //        $.each(item, function (k, v) {
    //    //            if (v || v === 0)
    //    //                item[k] = ko.observable(v);
    //    //            else
    //    //                item[k] = ko.observable("");
    //    //        });
    //    //        self.Items.push(item);
    //    //    });
    //    //});
    //}
}

NbuLib.extend(NbuLib.Core.AccountModule.UsersVM, NbuLib.Core.ListVM);

NbuLib.Core.AccountModule.UsersVM.prototype.ViewName = "Account_Admin_UsersGrid";
NbuLib.Core.AccountModule.UsersVM.prototype.remove = function (user) {
    var self = this;

    client.confirmDialog("Are you sure you want to delete '" + user.Email() + "' user from the system?", function (yes) {
        if (yes)
            client.deleteEntity("User", user.Id(), function (resp) {
                if (resp.success) {
                    client.closeDialog();
                    self.reload();
                }
            });
        //$.post('/api/account/DeleteUser', user, function (response) {
        //    if (response.success) {
        //        client.closeDialog();
        //        //self.Items.remove(user);
        //        self.reload();
        //    }
        //    else
        //        alert(response);
        //});
    });
}
NbuLib.Core.AccountModule.UsersVM.prototype.create = function () {
    var self = this;
    var vm = new NbuLib.Core.AccountModule.UserVM(null);
    vm.Loaded(true);
    client.showInDialog(vm, {
        templ: 'GeneralForm', title: 'Create User', act: {
            title: 'Create', callback: function (updates) {
                //newUser.UserGroup_UserGroup = ko.observable(updates.Entity.UserGroup_UserGroup);
                updates.save(function (response) {
                    if (response.success) {
                        self.reload();
                        client.closeDialog();
                    }
                    else
                        alert(response);
                });
            }
        }
    });
}
NbuLib.Core.AccountModule.UsersVM.prototype.open = function (user) {
    var self = this;

    self.Selected(user);
    //var popup = $('#modalWindow').modal('show');
    //popup.find('#myModalLabel').text("User Details");
    //var popupBody = popup.find('.modal-body')
    //    .attr('data-bind', "template: {name : 'Details'}");
    //ko.applyBindings(self, popupBody.get(0));

    var vm = new NbuLib.Core.EntityVM("Account_Admin_User");
    vm.loadData(user);

    client.showInDialog(vm, { templ: 'GeneralDetails', title: 'User Details' });
}
NbuLib.Core.AccountModule.UsersVM.prototype.edit = function (user) {
    var self = this;

    self.Selected(user);
    //var popup = $('#modalWindow').modal('show');
    //popup.find('#myModalLabel').text("Edit User");
    //var popupBody = popup.find('.modal-body')
    //    .attr('data-bind', "template: {name : 'UserForm'}");
    //ko.applyBindings(user, popupBody.get(0));
    //popup.find('.save').one('click', function (e) {
    //    e.preventDefault();
    //    $.post('/api/account/SaveUser', user, function (response) {
    //        console.log(response);
    //        if (response.success)
    //            popup.modal('hide');
    //        else
    //            alert(response);
    //    });
    //});

    //client.showInDialog(self, {
    //    templ: 'UserForm', title: 'Edit User', act: {
    //        title: 'Save', callback: function () {
    //            $.post('/api/account/SaveUser', user, function (response) {
    //                console.log(response);
    //                if (response.success)
    //                    client.closeDialog();
    //                else
    //                    alert(response);
    //            });
    //        }
    //    }
    //});

    var dialog = client.showInDialog(new NbuLib.Core.AccountModule.UserVM(null, user), {
        templ: 'GeneralForm', title: 'Edit User', act: {
            title: 'Save', callback: function (updates) {

                //self.Selected().UserGroup_UserGroup = updates.Entity.UserGroup_UserGroup;

                dialog.showProgress();
                updates.save(function (response) {
                    dialog.hideProgress();
                    if (response.success) {
                        setTimeout(function () {
                            dialog.close();
                            self.reload();
                        }, 200);
                    }
                });

                //$.post('/api/account/SaveUser', updates.Entity, function (response) {
                //    //console.log(response);
                //    if (response.success) {
                //        client.closeDialog();
                //        self.reload();
                //    }
                //    else
                //        alert(response);
                //});
            }
        }
    });
}
NbuLib.Core.AccountModule.UsersVM.prototype.changePwd = function (user) {
    var self = this;

    self.Selected(user);
    client.showInDialog(new NbuLib.Core.AccountModule.UserVM(null, user), {
        templ: 'UserChangePassword', title: 'Change Password', act: {
            title: 'Confirm', callback: function (updates) {
                var ins = client.getActiveDialog().find('input[type="password"]');
                if ($(ins[0]).val() != $(ins[1]).val()) {
                    client.showError('Password confirmation failed. Passwords must match!');
                    return;
                }
                var update = { Id: user.Id(), Entity: "User", Props: { Password: $(ins[0]).val() }, Rels: [] };
                client.update(update, function (response) {
                    //console.log(response);
                    if (response.success) {
                        client.closeDialog();
                        self.reload();
                    }
                    else
                        alert(response);
                });
            }
        }
    });
}



NbuLib.Core.AccountModule.UserVM = function (viewDefinition, data) {
    NbuLib.Core.AccountModule.UserVM.base.call(this, viewDefinition);
    if (data)
        this.load(data.Id(), this.EntityName);
    //if (data)
    //    this.loadData(data);
}

NbuLib.extend(NbuLib.Core.AccountModule.UserVM, NbuLib.Core.EntityVM);

NbuLib.Core.AccountModule.UserVM.prototype.ViewName = "Account_Admin_UserForm";
NbuLib.Core.AccountModule.UserVM.prototype.EntityName = "User";

NbuLib.Core.AccountModule.GroupsVM = function () {
    NbuLib.Core.AccountModule.GroupsVM.base.call(this);

    var self = this;

    //$.get("/api/account/ListGroups", {}, function (response) {
    //    self.loadItems(response);
    //});

    //self.loadAjax("/api/account/ListGroups");
    self.load({ Entity: 'UserGroup' });

    self.Actions.push({ Label: 'Edit', Callback: self.edit, Icon: 'icon-pencil' });
    self.Actions.push({ Label: 'Delete', Callback: self.remove, Icon: 'icon-trash' });
    self.GlobalActions.push({ Label: 'Create Group', Callback: self.create });

    //self.showUsers = function (group) {
    //    //var popup = $('#modalWindow').modal('show');
    //    //popup.find('#myModalLabel').text("Edit Group");
    //    //var popupBody = popup.find('.modal-body')
    //    //    .attr('data-bind', "template: {name : 'UsersGrid'}");
    //    //ko.applyBindings(, popupBody.get(0));

    //    client.showInDialog(new NbuLib.Core.AccountModule.UsersVM(group), { templ: UsersGrid, title: "Users in Group", width: 800 });
    //}

    //self.del = function (item) {
    //    $.post('/api/account/DeleteGroup', item, function (response) {
    //        if (response.success) {
    //            console.log("ok");
    //            self.Items.remove(item);
    //        }
    //        else
    //            alert(response);
    //    });
    //}

    //self.create = function () {
    //    var newGroup = {
    //        Name: ""
    //    };

    //    self.openEditForm(newGroup);
    //}
}

NbuLib.extend(NbuLib.Core.AccountModule.GroupsVM, NbuLib.Core.ListVM);

NbuLib.Core.AccountModule.GroupsVM.prototype.ViewName = "Account_Admin_UserGroupGrid";
NbuLib.Core.AccountModule.GroupsVM.prototype.create = function () {
    var self = this;
    var vm = new NbuLib.Core.AccountModule.GroupVM();
    vm.Loaded(true);
    client.showInDialog(vm, {
        templ: "GeneralForm", title: 'Create Group', act: {
            title: "Create", callback: function (data) {
                data.save(function (resp) {
                    if (resp.success) {
                        client.closeDialog();
                        //data.Entity.Id(resp.sync.Id);
                        //self.Items.push(data.Entity);
                        self.reload();
                    }
                    else {
                        alert(resp);
                    }
                });
            }
        }
    });
}
NbuLib.Core.AccountModule.GroupsVM.prototype.edit = function (group) {
    client.showInDialog(new NbuLib.Core.AccountModule.GroupVM(group), {
        templ: 'GeneralForm', title: 'Edit Group', act: {
            title: "Save", callback: function (update) {
                update.save(function () {
                    client.closeDialog();
                });
            }
        }
    });
}
NbuLib.Core.AccountModule.GroupsVM.prototype.remove = function (group) {
    var self = this;

    client.confirmDialog("Are you sure you want to delete '" + group.Name() + "' UserGroup?", function (yes) {
        if (yes)
            client.deleteEntity("UserGroup", group.Id(), function (resp) {
                if (resp.success) {
                    client.closeDialog();
                    //self.Items.remove(group);
                    self.reload();
                }
                else
                    alert(resp);
            });
    });
}

NbuLib.Core.AccountModule.GroupVM = function (group) {
    NbuLib.Core.AccountModule.GroupVM.base.call(this);

    var self = this;
    if (group)
        self.load(group.Id(), self.EntityName);

    //self.Permissions = ko.observableArray([]);

    //$.post('/api/Account/GetPermissions', group, function (perms) {
    //    //console.log(perms);

    //    $(perms.Active).each(function () {
    //        this.Active = ko.observable(true);//TODO Protected Observable
    //        this.Origin = true;
    //        self.Permissions.push(this);
    //    });

    //    $(perms.Inactive).each(function () {
    //        this.Active = ko.observable(false);//TODO Protected Observable
    //        this.Origin = false;
    //        self.Permissions.push(this);
    //    });

    //    self.loadData(group);
    //});

}

NbuLib.extend(NbuLib.Core.AccountModule.GroupVM, NbuLib.Core.EntityVM);

NbuLib.Core.AccountModule.GroupVM.prototype.EntityName = "UserGroup";
NbuLib.Core.AccountModule.GroupVM.prototype.ViewName = "Account_Admin_UserGroupForm";

//-------------------------


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

NbuLib.Core.AccountModule.PendingUsersVM.prototype.ViewName = "Account_Admin_PendingRegistrations";

NbuLib.Core.AccountModule.PendingUsersVM.prototype.approve = function (user) {
    var self = this;

    var update = { Id: user.Id(), Entity: "User", Props: { IsActive: true }, Rels: [] };
    client.update(update, function () {
        client.showSuccess("The registration of " + user.Email() + " was approved. The user is now active.");
        self.reload();
    });
};

NbuLib.Core.AccountModule.PendingUsersVM.prototype.decline = function (user) {
    var self = this;

    var update = { Id: user.Id(), Entity: "User", Props: { IsActive: true }, Rels: [] };
    client.update(update, function () {
        client.showSuccess("The registration of " + user.Email() + " was declined. An email was sent to the user to inform him that his account will be deleted.");
        self.reload();
    });
};


//Account module
(function () {
    client.hookMenuInit(function (menu) {
        menu.AddItem(new MenuViewModel({
            Name: 'Account',
            Items:
            [
                new MenuViewModel({
                    Name: 'Users', Action: function () {
                        client.showInViewport(new NbuLib.Core.AccountModule.UsersVM(), { templ: 'Grid' });
                    }
                }),
                new MenuViewModel({
                    Name: 'User Groups', Action: function () {
                        client.showInViewport(new NbuLib.Core.AccountModule.GroupsVM(), { templ: 'Grid' });
                    }
                }),
                new MenuViewModel({
                    Name: 'Pending Registrations', Action: function () {
                        client.showInViewport(new NbuLib.Core.AccountModule.PendingUsersVM(), { templ: 'Grid' });
                    }
                })
            ]
        }));

        menu.AddItemToMenu('Profile', new MenuViewModel({ Name: 'Change Password', Order: 1 }));


        //for (var x in menu.MenuItems()) {
        //    if (menu.MenuItems()[x].Name == 'Profile')
        //        menu.MenuItems()[x].MenuItems.push({Name : 'Change Password', Order : 1});
        //}
    });
})();
