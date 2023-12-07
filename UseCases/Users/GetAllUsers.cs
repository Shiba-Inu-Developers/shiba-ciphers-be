using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;

namespace my_new_app.UseCases.Users;

public class GetAllUsers
{
    public List<User> users;
    public GetAllUsers(List<User> users)
    { 
        this.users = users;
    }

}
