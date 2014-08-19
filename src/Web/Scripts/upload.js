


function fileModel(data) {

    var self = this;

    var alertSuccess = "alert alert-success",
        alertDanger = "alert alert-danger",
        alertInfo = "alert alert-info";

    this.xhr = {};

    this.name = ko.observable(data.name);
    this.size = ko.observable(data.size);
    this.type = ko.observable(data.type);
    this.file = ko.observable(data.file);
    this.isUploading = ko.observable(false);

    this.progress = ko.observable(0);


    this.error = ko.observable("");
    this.isError = ko.computed(function () {
        return self.error() != "";
    }, this);


    this.isComplete = ko.computed(function () {
        return self.progress() == 100;
    });


    this.alertCss = ko.computed(function() {
        return self.isComplete() ? alertSuccess : self.isError() ? alertDanger : alertInfo;
    });


    this.uploadProgress = function (evt) {
        if (evt.lengthComputable) {
            self.progress(Math.round(evt.loaded * 100 / evt.total));
        }
        else {
            self.progress("unable to compute");
        }
    }


    this.uploadFailed = function (evt) {
        self.error("There was an error attempting to upload the file.");
        self.done();
    };

    this.uploadComplete = function (evt) {
        self.done();
    };

    this.uploadCanceled = function (evt) {
        self.error("The upload has been canceled by the user or the connection dropped.");
        self.xhr.abort();
        self.done();
    };

    this.startUpload = function() {

        self.isUploading(true);
        
        var fd = new FormData();
        fd.append("id", "123");
        fd.append("fileToUpload", self.file());
        
        var request = new XMLHttpRequest();
        request.upload.addEventListener("progress", self.uploadProgress, false);
        request.addEventListener("load", self.uploadComplete, false);
        request.addEventListener("error", self.uploadFailed, false);
        request.addEventListener("abort", self.uploadCanceled, false);
        
        request.open("POST", "/api/upload");
        request.send(fd);

        self.xhr = request;
    };

    this.done = function() {
        self.isUploading(false);
        self.xhr = {};
    };
}




var viewModel = function () {

    var self = this;

    this.files = ko.observableArray([]);


    this.addFileItem = function (elem) {
        if (elem.nodeType === 1) {
            $(elem).hide().fadeIn();
        }
    };


    this.fadeOutFileItem = function (elem) {
        if (elem.nodeType === 1) {
            $(elem).fadeOut(function () { $(elem).remove(); });
        }
    };


    this.removeFileClick = function (elem) {
        self.files.remove(elem);
    };


    this.fileSelected = function (files) {

        var self = this;
        for (var i = 0; i < files.length; i++) {
            if (files[i]) {
                var fileSize = 0;
                if (files[i].size > 1024 * 1024)
                    fileSize = (Math.round(files[i].size * 100 / (1024 * 1024)) / 100).toString() + 'MB';
                else
                    fileSize = (Math.round(files[i].size * 100 / 1024) / 100).toString() + 'KB';

                self.files.push(new fileModel({ name: files[i].name, size: fileSize, type: files[i].type, file: files[i] }));
            }
        }
    };

    this.upload = function () {
        var self = this;

        for (var i = 0; i < self.files().length; i++) {
            self.files()[i].startUpload();
        }
    };
};



var vm = new viewModel();
ko.applyBindings(vm);



