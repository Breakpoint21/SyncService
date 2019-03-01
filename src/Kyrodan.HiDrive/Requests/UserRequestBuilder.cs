namespace Kyrodan.HiDrive.Requests
{
    internal class UserRequestBuilder : BaseRequestBuilder, IUserRequestBuilder
    {
        public UserRequestBuilder(IBaseClient client)
            : base("user", client)
        {
        }

        public IUserMeRequestBuilder Me
        {
            get { return new UserMeRequestBuilder(AppendSegmentToRequestUrl("me"), Client); }
        }
    }
}