﻿using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfTextReader.PDFCore;
using PdfTextReader.TextStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PdfTextReader.Base;

namespace PdfTextReader.Execution
{
    class PipelineInputPdf : IPipelinePdfContext, IDisposable
    {
        private readonly string _input;
        private PdfDocument _pdfDocument;
        private string _output;
        private PdfDocument _pdfOutput;

        public PipelineInputPdfPage CurrentPage { get; private set; }
        
        private List<IDisposable> _disposableObjects = new List<IDisposable>();

        public object CurrentText { get; private set; }
        public void SetCurrentText<T>(PipelineText<T> pipeText)
        {
            ReleaseAfterFinish(pipeText);

            CurrentText = pipeText;
        }

        public void ReleaseAfterFinish(object instance)
        {
            var disposableObj = instance as IDisposable;
            if (disposableObj != null)
            {
                _disposableObjects.Add(disposableObj);
            }
        }

        public PipelineInputPdf(string filename)
        {
            var pdfDocument = new PdfDocument(new PdfReader(filename));

            Console.WriteLine($"Filename={filename}");

            this._input = filename;
            this._pdfDocument = pdfDocument;
        }
        
        public PipelineInputPdfPage Page(int pageNumber)
        {
            Console.WriteLine($"  page: {pageNumber}");

            var page = new PipelineInputPdfPage(this, pageNumber);

            if( CurrentPage != null )
            {
                CurrentPage.Dispose();
            }

            CurrentPage = page;

            return page;
        }

        public PipelineInputPdf Output(string outfile)
        {
            if( _pdfOutput != null )
            {
                ((IDisposable)_pdfOutput).Dispose();
            }

            var pdfOutput = new PdfDocument(new PdfReader(_input), new PdfWriter(outfile));

            this._output = outfile;
            this._pdfOutput = pdfOutput;

            return this;
        }

        public void Dispose()
        {
            if( CurrentPage != null )
            {
                CurrentPage.Dispose();
                CurrentPage = null;
            }

            if (_pdfDocument != null)
            {
                ((IDisposable)_pdfDocument).Dispose();
                _pdfDocument = null;
            }

            if (_pdfOutput != null)
            {
                ((IDisposable)_pdfOutput).Dispose();
                _pdfOutput = null;
            }
            
            lock (_disposableObjects)
            {
                if (_disposableObjects != null)
                {
                    foreach (var obj in _disposableObjects)
                    {
                        obj.Dispose();
                    }

                    _disposableObjects = null;
                }
            }
        }

        public void Extract(string outfile, int start, int end)
        {
            IList<int> pageNumbers = Enumerable.Range(start, end - start + 1).ToList();

            using (var pdfInput = new PdfDocument(new PdfReader(_input)) )
            using (var pdfOutput = new PdfDocument(new PdfWriter(outfile)))
            {
                pdfInput.CopyPagesTo(pageNumbers, pdfOutput);                
            }
        }

        public void AllPages(Action<PipelineInputPdfPage> callback)
        {
            int totalPages = _pdfDocument.GetNumberOfPages();

            for (int i=1; i<=totalPages; i++)
            {
                var pdfPage = Page(i);

                callback(pdfPage);
            }
        }

        public PipelineText<TextLine> AllPages<T>(Action<PipelineInputPdfPage> callback)
            where T : IConvertBlock, new()
        {
            var textLines = StreamConvert<T>(callback);
            
            var pipeText = new PipelineText<TextLine>(this, textLines);
            
            return pipeText;
        }

        public IEnumerable<TextLine> StreamConvert<T>(Action<PipelineInputPdfPage> callback)
            where T: IConvertBlock, new()
        {
            var processor = new T();            

            int totalPages = _pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= totalPages; i++)
            {
                var pdfPage = Page(i);

                callback(pdfPage);

                var textSet = processor.ConvertBlock(CurrentPage.GetLastResult());

                foreach(var t in textSet.AllText)
                {
                    yield return t;
                }
            }
        }

        public class PipelineInputPdfPage : IDisposable
        {
            private readonly PipelineInputPdf _pdf;
            private readonly int _pageNumber;            
            private readonly PdfPage _pdfPage;
            private PipelinePage _page;
            private PdfCanvas _outputCanvas;

            private PipelinePageFactory _factory = new PipelinePageFactory();

            public BlockPage GetLastResult() => _page.LastResult;

            public PipelineInputPdfPage(PipelineInputPdf pipelineInputContext, int pageNumber)
            {
                var pdfPage = pipelineInputContext._pdfDocument.GetPage(pageNumber);

                this._pdf = pipelineInputContext;
                this._pageNumber = pageNumber;
                this._pdfPage = pdfPage;
            }

            public T CreateInstance<T>()
                where T: new()
            {
                return _factory.CreateInstance<T>();
            }

            public PipelinePage ParsePdf<T>()
                where T: IEventListener, IPipelineResults<BlockPage>, new()
            {
                var listener = CreateInstance<T>();

                var parser = new PdfCanvasProcessor(listener);
                parser.ProcessPageContent(_pdfPage);

                var page = new PipelinePage(_pdf,  _pageNumber);

                page.LastResult = listener.GetResults();

                if (page.LastResult == null)
                    throw new InvalidOperationException();

                if (page.LastResult.AllBlocks == null)
                    throw new InvalidOperationException();

                _page = page;

                return page;
            }

            public PipelineInputPdfPage Output(string filename)
            {
                this._pdf.Output(filename);
                return this;
            }

            PdfCanvas GetCanvas()
            {
                if(_outputCanvas == null)
                {
                    var page = _pdf._pdfOutput.GetPage(_pageNumber);                                        
                    var canvas = new PdfCanvas(page);

                    _outputCanvas = canvas;
                }                

                return _outputCanvas;
            }

            iText.Kernel.Colors.DeviceRgb GetColor(System.Drawing.Color color)
            {
                return new iText.Kernel.Colors.DeviceRgb(color.R, color.G, color.B);
            }

            public void DrawRectangle(double x, double h, double width, double height, System.Drawing.Color color)
            {
                var canvas = GetCanvas();

                var pdfColor = GetColor(color);

                canvas.SetStrokeColor(pdfColor);
                canvas.Rectangle(x, h, width, height);
                canvas.Stroke();
            }
            public void DrawLine(double x1, double h1, double x2, double h2, System.Drawing.Color color)
            {
                var canvas = GetCanvas();

                var pdfColor = GetColor(color);

                canvas.SetStrokeColor(pdfColor);
                canvas.MoveTo(x1, h1);
                canvas.LineTo(x2, h2);
                canvas.Stroke();
            }
            

            public void Dispose()
            {
                if( _outputCanvas != null )
                {
                    _outputCanvas.Release();
                    _outputCanvas = null;
                }
            }
        }        
    }
}
