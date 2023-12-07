namespace my_new_app.UseCases.Users.Interfaces;
using my_new_app.Model;
public interface IGetAllUsers {
    Task<List<User>> GetAllUsers();
}
 public interface IUserRepository
 {    Task<List<User>> GetAllUsers();
           
 }