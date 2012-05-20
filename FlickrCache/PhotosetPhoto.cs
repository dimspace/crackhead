using System;
namespace FlickrCache
{
    /// <summary>
    /// Implement the Equals and GetHashCode method using Id.
    /// </summary>
    public partial class PhotosetPhoto {

        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals (this, obj))
                return true;
            if (obj.GetType () != typeof(PhotosetPhoto))
                return false;
            PhotosetPhoto other = (PhotosetPhoto)obj;
            return Id == other.Id;
        }
        

        public override int GetHashCode ()
        {
            unchecked {
                return (Id != null ? Id.GetHashCode () : 0);
            }
        }
        

    }
}

