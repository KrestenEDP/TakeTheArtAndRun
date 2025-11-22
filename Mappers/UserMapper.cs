using artapi.DTOs;
using artapi.Models;

namespace artapi.Mappers;

public static class UserMapper
{
    public static UserReadDto ToDto(this User user)
    {
        return new UserReadDto
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            Role = user.Role.ToString()
        };
    }
}
