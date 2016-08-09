using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common.Internal;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IThrottlingReporter
    {
        void ReportThrottling(string delay, string expiration);
    }

    public class ThrottlingReportHandler : DelegatingHandler
    {
        private IThrottlingReporter _throttlingReporter;

        public ThrottlingReportHandler(IThrottlingReporter throttlingReporter)
            : base()
        {
            ArgUtil.NotNull(throttlingReporter, nameof(throttlingReporter));
            _throttlingReporter = throttlingReporter;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Call the inner handler.
            var response = await base.SendAsync(request, cancellationToken);

            // Inspect whether response has throttling information
            IEnumerable<string> vssRequestDelayed = null;
            IEnumerable<string> vssRequestQuotaReset = null;
            if (response.Headers.TryGetValues(HttpHeaders.VssRequestDelayed, out vssRequestDelayed) &&
                response.Headers.TryGetValues(HttpHeaders.VssRequestQuotaReset, out vssRequestQuotaReset) &&
                !string.IsNullOrEmpty(vssRequestDelayed.FirstOrDefault()) &&
                !string.IsNullOrEmpty(vssRequestQuotaReset.FirstOrDefault()))
            {
                _throttlingReporter.ReportThrottling(vssRequestDelayed.First(), vssRequestQuotaReset.First());
            }

            return response;
        }
    }
}