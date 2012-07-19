namespace RavenFS.Synchronization.Multipart
{
	using System.IO;
	using System.Net;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Threading.Tasks;
	using RavenFS.Infrastructure;

	public class SeedFilePart : HttpContent
	{
		public SeedFilePart(long from, long to)
		{
			Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
			Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue(SyncingMultipartConstants.NeedType, SyncingNeedType));
			Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue(SyncingMultipartConstants.RangeFrom, @from.ToString()));
			Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue(SyncingMultipartConstants.RangeTo, to.ToString()));

			Headers.ContentType = new MediaTypeHeaderValue("plain/text");
		}

		public string SyncingNeedType
		{
			get { return "seed"; }
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			return new CompletedTask();
		}

		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return true;
		}
	}
}