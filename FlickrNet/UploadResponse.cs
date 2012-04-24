using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace FlickrNet
{
    /// <summary>
    /// Information returned by the UploadPicture url.
    /// </summary>
    [XmlRoot("rsp")]
    public class UploadResponse
    {
        private ResponseStatus status;
        private string photoId;
        private string ticketId;
        private ResponseError error;

        /// <summary>
        /// The status of the upload, either "ok" or "fail".
        /// </summary>
        [XmlAttribute("stat", Form = XmlSchemaForm.Unqualified)]
        public ResponseStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        /// <summary>
        /// If the upload succeeded then this contains the id of the photo. Otherwise it will be zero.
        /// </summary>
        [XmlElement("photoid", Form = XmlSchemaForm.Unqualified)]
        public string PhotoId
        {
            get { return photoId; }
            set { photoId = value; }
        }

        /// <summary>
        /// The ticket id, if using Asynchronous uploading.
        /// </summary>
        [XmlElement("ticketid", Form = XmlSchemaForm.Unqualified)]
        public string TicketId
        {
            get { return ticketId; }
            set { ticketId = value; }
        }

        /// <summary>
        /// Contains the error returned if the upload is unsuccessful.
        /// </summary>
        [XmlElement("err", Form = XmlSchemaForm.Unqualified)]
        public ResponseError Error
        {
            get { return error; }
            set { error = value; }
        }
    }

    /// <summary>
    /// If an error occurs then Flickr returns this object.
    /// </summary>
    [System.Serializable]
    public class ResponseError
    {
        /// <summary>
        /// The code or number of the error.
        /// </summary>
        /// <remarks>
        /// 100 - Invalid Api Key.
        /// 99  - User not logged in.
        /// Other codes are specific to a method.
        /// </remarks>
        [XmlAttribute("code", Form = XmlSchemaForm.Unqualified)]
        public int Code { get; set; }

        /// <summary>
        /// The verbose message matching the error code.
        /// </summary>
        [XmlAttribute("msg", Form = XmlSchemaForm.Unqualified)]
        public string Message { get; set; }

        /// <summary>
        /// The default constructor for a response error.
        /// </summary>
        public ResponseError()
        {
        }

        /// <summary>
        /// Constructor for a response error using the error code and message returned by Flickr.
        /// </summary>
        /// <param name="code">The error code returned by Flickr.</param>
        /// <param name="message">The error message returned by Flickr.</param>
        public ResponseError(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }

    /// <summary>
    /// The status of the response, either ok or fail.
    /// </summary>
    [Serializable]
    public enum ResponseStatus
    {
        /// <summary>
        /// An unknown status, and the default value if not set.
        /// </summary>
        [XmlEnum("unknown")]
        Unknown,

        /// <summary>
        /// The response returns "ok" on a successful execution of the method.
        /// </summary>
        [XmlEnum("ok")]
        Ok,
        /// <summary>
        /// The response returns "fail" if there is an error, such as invalid API key or login failure.
        /// </summary>
        [XmlEnum("fail")]
        Failed
    }

}
