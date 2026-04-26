package com.factorypulse.auth.config;

import com.factorypulse.auth.model.User;
import com.factorypulse.auth.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.crypto.password.PasswordEncoder;

@Configuration
@RequiredArgsConstructor
@Slf4j
public class DataSeeder {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;

    @Bean
    CommandLineRunner seedDefaultAdmin() {
        return args -> {
            if (userRepository.count() > 0) {
                log.debug("Users already exist, skipping seed.");
                return;
            }

            var admin = new User();
            admin.setUsername("admin");
            admin.setPasswordHash(passwordEncoder.encode("Admin1234!"));
            admin.setFullName("System Admin");
            admin.setRole(User.Role.ADMIN);

            userRepository.save(admin);

            log.warn("=================================================================");
            log.warn("  Default admin user created: admin / Admin1234!");
            log.warn("  CHANGE THIS PASSWORD IMMEDIATELY in any non-local environment.");
            log.warn("=================================================================");
        };
    }
}
