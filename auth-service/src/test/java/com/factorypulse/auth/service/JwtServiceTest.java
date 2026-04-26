package com.factorypulse.auth.service;

import com.factorypulse.auth.model.User;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.*;

class JwtServiceTest {

    // Must be >= 32 chars for HS256
    private static final String SECRET = "test-secret-key-min-32-chars-long!!";

    private JwtService jwtService;

    @BeforeEach
    void setUp() {
        jwtService = new JwtService(SECRET, 3_600_000L); // 1 hour
    }

    private User makeUser(String username, User.Role role) {
        var u = new User();
        u.setId(1L);
        u.setUsername(username);
        u.setPasswordHash("x");
        u.setFullName("Test User");
        u.setRole(role);
        return u;
    }

    @Test
    void generateAndValidate_roundtrip() {
        var user  = makeUser("engineer1", User.Role.ENGINEER);
        var token = jwtService.generateToken(user);

        assertThat(token).isNotBlank();
        assertThat(jwtService.isValid(token)).isTrue();
        assertThat(jwtService.getUsernameFrom(token)).isEqualTo("engineer1");
    }

    @Test
    void claims_containRole() {
        var user   = makeUser("admin1", User.Role.ADMIN);
        var token  = jwtService.generateToken(user);
        var claims = jwtService.parseToken(token);

        assertThat(claims.get("role")).isEqualTo("ADMIN");
    }

    @Test
    void expiredToken_isInvalid() throws InterruptedException {
        var shortLived = new JwtService(SECRET, 1L); // 1 ms
        var user       = makeUser("user1", User.Role.VIEWER);
        var token      = shortLived.generateToken(user);

        Thread.sleep(50);
        assertThat(shortLived.isValid(token)).isFalse();
    }

    @Test
    void tamperedToken_isInvalid() {
        var user  = makeUser("user1", User.Role.VIEWER);
        var token = jwtService.generateToken(user);
        // Flip a character in the signature
        var tampered = token.substring(0, token.length() - 4) + "XXXX";

        assertThat(jwtService.isValid(tampered)).isFalse();
    }

    @Test
    void garbage_isInvalid() {
        assertThat(jwtService.isValid("not.a.jwt")).isFalse();
        assertThat(jwtService.isValid("")).isFalse();
    }
}
