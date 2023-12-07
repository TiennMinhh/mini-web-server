﻿using MiniWebServer.Helpers;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace MiniWebServer.Abstractions.Http.Form
{
    public class XWwwFormUrlencodedFormReader(long contentLength) : IFormReader
    {
        public async Task<IRequestForm?> ReadAsync(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            StringBuilder stringBuilder = new();
            long bytesRead = 0;

            ReadResult readResult = await pipeReader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            while (bytesRead < contentLength)
            {
                long maxBytesToRead = Math.Min(contentLength, buffer.Length);

                stringBuilder.Append(Encoding.ASCII.GetString(buffer.Slice(0, maxBytesToRead)));

                bytesRead += maxBytesToRead;
                pipeReader.AdvanceTo(buffer.GetPosition(maxBytesToRead));

                if (bytesRead < contentLength)
                {
                    readResult = await pipeReader.ReadAsync(cancellationToken);
                    buffer = readResult.Buffer;
                }
            }

            var form = new RequestForm();

            // now we have read the content, it's time to decode
            string[] strings = UrlHelpers.UrlDecode(stringBuilder.ToString()).Split(['&']);
            foreach (string s in strings)
            {
                int idx = s.IndexOf('=');
                if (idx < 0)
                {
                    continue; // we accept some minor errors
                }
                else if (idx == 0)
                {
                    continue; // we accept some minor errors
                }
                else
                {
                    form[s[..idx]] = s[(idx + 1)..];
                }
            }

            return form;
        }
    }
}
