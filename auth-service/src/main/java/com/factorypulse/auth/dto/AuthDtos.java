package com.factorypulse.auth.dto;

import com.factorypulse.auth.model.User;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;

public class AuthDtos {

    @Data
    public static class LoginRequest {
        @NotBlank
        private String username;

        @NotBlank
        private String password;
    }

    @Data
    public static class LoginResponse {
        private String token;
        private String username;
        private String fullName;
        private String role;

        public LoginResponse(String token, User user) {
            this.token    = token;
            this.username = user.getUsername();
            this.fullName = user.getFullName();
            this.role     = user.getRole().name();
        }
    }

    @Data
    public static class RegisterRequest {
        @NotBlank
        @Size(min = 3, max = 100)
        private String username;

        @NotBlank
        @Size(min = 8)
        private String password;

        @NotBlank
        private String fullName;

        private User.Role role = User.Role.VIEWER;
    }

    @Data
    public static class UserResponse {
        private Long id;
        private String username;
        private String fullName;
        private String role;
        private boolean enabled;

        public UserResponse(User u) {
            this.id       = u.getId();
            this.username = u.getUsername();
            this.fullName = u.getFullName();
            this.role     = u.getRole().name();
            this.enabled  = u.isEnabled();
        }
    }
}
