package com.factorypulse.auth.service;

import com.factorypulse.auth.dto.AuthDtos.RegisterRequest;
import com.factorypulse.auth.model.User;
import com.factorypulse.auth.repository.UserRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;

import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class UserServiceTest {

    @Mock UserRepository userRepository;
    @Mock PasswordEncoder passwordEncoder;

    @InjectMocks UserService userService;

    @BeforeEach
    void setUp() {
        when(passwordEncoder.encode(anyString())).thenReturn("hashed-password");
    }

    @Test
    void createUser_success() {
        when(userRepository.existsByUsername("jsmith")).thenReturn(false);
        when(userRepository.save(any())).thenAnswer(inv -> {
            var u = (User) inv.getArgument(0);
            u.setId(1L);
            return u;
        });

        var req = new RegisterRequest();
        req.setUsername("jsmith");
        req.setPassword("securePass1");
        req.setFullName("John Smith");
        req.setRole(User.Role.ENGINEER);

        var user = userService.createUser(req);

        assertThat(user.getUsername()).isEqualTo("jsmith");
        assertThat(user.getFullName()).isEqualTo("John Smith");
        assertThat(user.getRole()).isEqualTo(User.Role.ENGINEER);
        assertThat(user.getPasswordHash()).isEqualTo("hashed-password");
        verify(userRepository).save(any(User.class));
    }

    @Test
    void createUser_duplicateUsername_throwsException() {
        when(userRepository.existsByUsername("jsmith")).thenReturn(true);

        var req = new RegisterRequest();
        req.setUsername("jsmith");
        req.setPassword("pass");
        req.setFullName("John Smith");

        assertThatThrownBy(() -> userService.createUser(req))
            .isInstanceOf(IllegalArgumentException.class)
            .hasMessageContaining("already taken");
    }

    @Test
    void createUser_defaultsToViewerRole() {
        when(userRepository.existsByUsername(anyString())).thenReturn(false);
        when(userRepository.save(any())).thenAnswer(inv -> inv.getArgument(0));

        var req = new RegisterRequest();
        req.setUsername("newuser");
        req.setPassword("pass1234");
        req.setFullName("New User");
        req.setRole(null);  // explicitly not set

        var user = userService.createUser(req);
        assertThat(user.getRole()).isEqualTo(User.Role.VIEWER);
    }

    @Test
    void loadUserByUsername_found() {
        var mockUser = new User();
        mockUser.setUsername("admin");
        when(userRepository.findByUsername("admin")).thenReturn(Optional.of(mockUser));

        var result = userService.loadUserByUsername("admin");
        assertThat(result.getUsername()).isEqualTo("admin");
    }

    @Test
    void loadUserByUsername_notFound_throws() {
        when(userRepository.findByUsername("nobody")).thenReturn(Optional.empty());

        assertThatThrownBy(() -> userService.loadUserByUsername("nobody"))
            .hasMessageContaining("nobody");
    }
}
