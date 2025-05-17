namespace TrainingManagementSystem.ViewModels.Identity
{
    public class UserClaimListVM
    {
        public UserClaimListVM()
        {
            Claims = new List<UserClaimVM>();
        }

        public string UserId { get; set; }
        public List<UserClaimVM> Claims { get; set; }

    }
}
