mergeInto(LibraryManager.library, {

    InternalUtilsWebGLProxyOpenURL: function (urlPointer, targetPointer) {

        var url = Pointer_stringify(urlPointer);
        var target = Pointer_stringify(targetPointer);
        console.log("Unity player try to open url " + url + " with target " + target);

        if (target == "_self") {
          window.parent.location.href = url;
        } else {
          window.open(url, target);
        }

    }

});