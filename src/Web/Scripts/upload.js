


function fileModel(data) {

    var self = this;

    this.name = ko.observable(data.name);
    this.size = ko.observable(data.size);
    this.type = ko.observable(data.type);
    this.file = ko.observable(data.file);
    this.progress = ko.observable(0);

    this.error = ko.observable("");
    this.isError = ko.computed(function () {
        return self.error() != "";
    }, this);

    this.isComplete = ko.computed(function() {
        return self.progress() == 100;
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
    };

    this.uploadComplete = function (evt) {
        //self.error("Upload complete!");
    };

    this.uploadCanceled = function (evt) {
        self.error("The upload has been canceled by the user or the browser dropped the connection.");
    };
}


var viewModel = function () {
    var self = this;

    this.files = ko.observableArray([]);

    this.add = function (elem) {
        if (elem.nodeType === 1) {
            $(elem).hide().fadeIn();
        }
    };

    this.remove = function (elem) {
        if (elem.nodeType === 1) {
            $(elem).fadeOut(function () { $(elem).remove(); });
        }
    };

    this.removeFile = function (elem) {
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

                self.files.push(new fileModel({
                    name: files[i].name,
                    size: fileSize,
                    type: files[i].type,
                    file: files[i]
                }));
            }
        }
    };

    this.upload = function () {

        var self = this;

        for (var i = 0; i < self.files().length; i++) {
            var xhr = new XMLHttpRequest();
            var file = self.files()[i];

            var fd = new FormData();
            fd.append("id", "123"); 
            fd.append("fileToUpload", file.file());

            /* event listners */
            xhr.upload.addEventListener("progress", file.uploadProgress, false);
            xhr.addEventListener("load", file.uploadComplete, false);
            xhr.addEventListener("error", file.uploadFailed, false);
            xhr.addEventListener("abort", file.uploadCanceled, false);


            xhr.open("POST", "/api/upload");
            xhr.send(fd);
        }
    };
};



var vm = new viewModel();
ko.applyBindings(vm);



