using Microsoft.AspNetCore.Identity;

namespace EventApp.Models
{
    // The ApplicationUser class extends the default IdentityUser
    // This is where you would add custom profile fields if needed, 
    // such as a FirstName, LastName, or ProfilePicture.
    public class ApplicationUser : IdentityUser
    {
        // For now, we will leave it empty as the assignment doesn't require extra fields,
        // but this class is mandatory for customizing Identity.
    }
}