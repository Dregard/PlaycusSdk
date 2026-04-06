mergeInto(LibraryManager.library, {

    InternalGDPRGetVersionTOS: function () {

        var data = 0;

        if (typeof getVersionTos !== "undefined") {
          if (typeof getVersionTos === "function") {
            data = getVersionTos();
          }
        }

        data = data + ""; //(sic!) This is not typo or mistake, it is really needed for properly work of proxy

        var length = lengthBytesUTF8(data) + 1;
        var buffer = _malloc(length);

        stringToUTF8(data, buffer, length);

        return buffer;

    }

});