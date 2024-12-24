import * as pdfjslib from '/lib/pdfjs/pdf.js';
pdfjsLib.GlobalWorkerOptions.workerSrc = '/lib/pdfjs/pdf.worker.js';

export function pdfHandling()  {

    var instance = {};

    const delay = ms => new Promise(res => setTimeout(res, ms));

    instance.generateThumbnail = async function (url) {

        const documentLoadingTask = await pdfjsLib.getDocument(url);
        var pdf = await documentLoadingTask.promise;
        var page = await pdf.getPage(1);

        var scale = 0.5;
        var viewport = page.getViewport({ scale: scale });

        var canvas = document.createElement("canvas");
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        
        var context = canvas.getContext('2d');
        context.clearRect(0, 0, canvas.width, canvas.height);

        console.log("rendering");
        await page.render({ canvasContext: context, viewport: viewport });

        await delay(2000);

        var dataUri = canvas.toDataURL("image/jpeg", 0.6);

        canvas.remove();
        return dataUri;
    };

    return instance;

}