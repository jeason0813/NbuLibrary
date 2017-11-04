define('home',
    [],
    function () {

        var MessageTypes = {
            Info: 0,
            Warning: 1,
            Error: 2,
            Success : 3
        };


        var _messages = ko.observableArray();

        function addMessage(title, message, icon, type) {
            _messages.push({ Title: title, Message: message, Icon: icon, Type: type });
        }

        return {
            template: 'Home',
            Messages: _messages,
            addInfo: function (title, msg, icon) { addMessage(title, msg, icon, MessageTypes.Info); },
            dismiss: function (msg)
            {
                _messages.remove(msg);
            }
        };
    });