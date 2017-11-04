/// <reference path="../knockout-2.2.1.debug.js" />

define('vm.user',
    ['dataservice'],
    function (dataservice) {
        this.template = 'core_user';

        this.Email = ko.observable();
        this.UserType = ko.observable();
        this.FirstName = ko.observable();
        this.LastName = ko.observable();
        this.MiddleName = ko.observable();
        this.FacultyNumber = ko.observable();
        this.CardNumber = ko.observable();
        this.PhoneNumber = ko.observable();

        var self = this;
        function bind(id) {
            $.when(dataservice.get('User', id))
            .done(function (user) {

                //todo: mapper from schema!
                self.Email(user.Email);
                self.UserType(user.UserType);
                self.FirstName(user.FirstName);
                self.LastName(user.LastName);
                self.MiddleName(user.MiddleName);
                self.FacultyNumber(user.FacultyNumber);
                self.CardNumber(user.CardNumber);
                self.PhoneNumber(user.PhoneNumber);
            });
        };

        function edit()
        {
        }

        return {
            //TODO? 
            Email: this.Email,
            UserType: this.UserType,
            FirstName: this.FirstName,
            LastName: this.LastName,
            MiddleName: this.MiddleName,
            FacultyNumber: this.FacultyNumber,
            CardNumber: this.CardNumber,
            PhoneNumber: this.PhoneNumber,
            bind: bind
        };
});