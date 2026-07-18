document.addEventListener('DOMContentLoaded', () => {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // 1. Theme Toggle Configuration
    const toggle = document.getElementById('theme-toggle');
    if (toggle) {
        toggle.addEventListener('click', () => {
            const next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
            document.documentElement.dataset.theme = next;
            localStorage.setItem('apsra-theme', next);
        });
    }

    // 2. Sticky & Auto-Hiding Navigation Bar
    const header = document.querySelector('.site-header');
    if (header) {
        let lastScrollTop = 0;
        const scrollThreshold = 10;
        
        // Initial state check
        if (window.pageYOffset > 50) {
            header.classList.add('nav-scrolled');
        }

        window.addEventListener('scroll', () => {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            // Background blur styling when scrolled
            if (scrollTop > 50) {
                header.classList.add('nav-scrolled');
            } else {
                header.classList.remove('nav-scrolled');
            }

            // Hide/Show on scroll direction
            if (Math.abs(lastScrollTop - scrollTop) <= scrollThreshold) {
                return;
            }

            if (scrollTop > lastScrollTop && scrollTop > 100) {
                // Scrolling down - hide navigation bar
                header.classList.add('nav-hidden');
            } else {
                // Scrolling up - show navigation bar
                header.classList.remove('nav-hidden');
            }

            lastScrollTop = scrollTop;
        }, { passive: true });
    }

    // 3. Dynamic Button Click Ripple Effects
    const rippleButtons = document.querySelectorAll('.btn-brand, .btn-quiet, .btn-premium, .nav-cta');
    rippleButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            // Find coordinates relative to the button
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            // Create ripple span element
            const ripple = document.createElement('span');
            ripple.className = 'btn-ripple';
            ripple.style.left = `${x}px`;
            ripple.style.top = `${y}px`;

            this.appendChild(ripple);

            // Clean up ripple element after animation finishes
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });

    // 4. Interactive Background Particle Animation Engine
    const canvas = document.getElementById('particle-canvas');
    if (canvas && !prefersReducedMotion) {
        const ctx = canvas.getContext('2d');
        let particles = [];
        let animationFrameId;
        
        // Mouse state tracking
        const mouse = {
            x: null,
            y: null,
            radius: 120 // Interaction distance
        };
        
        window.addEventListener('mousemove', (e) => {
            mouse.x = e.clientX;
            mouse.y = e.clientY;
        });
        
        window.addEventListener('mouseleave', () => {
            mouse.x = null;
            mouse.y = null;
        });
        
        // Adapt configuration dynamically to screen width
        const getParticleConfig = () => {
            const width = window.innerWidth;
            if (width < 600) {
                return { count: 50, maxLineDist: 70 }; // mobile
            } else if (width < 1000) {
                return { count: 80, maxLineDist: 85 }; // tablet
            } else {
                return { count: 140, maxLineDist: 100 }; // desktop
            }
        };
        
        let config = getParticleConfig();
        
        // Helper to grab theme-adaptive particle color
        const getParticleColor = () => {
            const isDark = document.documentElement.dataset.theme === 'dark';
            // Return color arrays [R, G, B] matching var(--mint-strong)
            return isDark ? [84, 224, 180] : [45, 212, 165];
        };
        
        let activeColor = getParticleColor();
        
        // Watch for theme changes (so particles update color on theme switch!)
        const themeObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.attributeName === 'data-theme') {
                    activeColor = getParticleColor();
                }
            });
        });
        themeObserver.observe(document.documentElement, { attributes: true });
        
        class Particle {
            constructor() {
                this.x = Math.random() * canvas.width;
                this.y = Math.random() * canvas.height;
                this.size = Math.random() * 2.2 + 0.8; // Varying size 0.8px - 3px
                this.opacity = Math.random() * 0.55 + 0.15; // Varying opacity 0.15 - 0.7
                
                // Slow floating velocities
                this.vx = (Math.random() - 0.5) * 0.4;
                this.vy = (Math.random() - 0.5) * 0.4;
                
                // Track individual random drift properties to create natural fluid wave float
                this.driftSpeed = Math.random() * 0.02 + 0.005;
                this.driftAngle = Math.random() * Math.PI * 2;
                
                this.maxSpeed = Math.random() * 0.6 + 0.4;
            }
            
            update() {
                // Apply mouse interaction (attraction)
                if (mouse.x !== null) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    
                    if (distance < mouse.radius) {
                        const force = (mouse.radius - distance) / mouse.radius;
                        // Gentle pull towards the cursor
                        this.vx += (dx / distance) * force * 0.06;
                        this.vy += (dy / distance) * force * 0.06;
                    }
                }
                
                // Slow down/Friction to keep movement elegant
                this.vx *= 0.96;
                this.vy *= 0.96;
                
                // Continuous slow floating drift
                this.driftAngle += this.driftSpeed;
                this.vx += Math.cos(this.driftAngle) * 0.015;
                this.vy += Math.sin(this.driftAngle) * 0.015;
                
                // Speed limit cap
                const speed = Math.sqrt(this.vx * this.vx + this.vy * this.vy);
                if (speed > this.maxSpeed) {
                    this.vx = (this.vx / speed) * this.maxSpeed;
                    this.vy = (this.vy / speed) * this.maxSpeed;
                }
                
                this.x += this.vx;
                this.y += this.vy;
                
                // Screen boundaries wrapping
                if (this.x < 0) this.x = canvas.width;
                if (this.x > canvas.width) this.x = 0;
                if (this.y < 0) this.y = canvas.height;
                if (this.y > canvas.height) this.y = 0;
            }
            
            draw() {
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fillStyle = `rgba(${activeColor[0]}, ${activeColor[1]}, ${activeColor[2]}, ${this.opacity})`;
                
                // Faint glow for larger particles
                if (this.size > 2.0) {
                    ctx.shadowBlur = 6;
                    ctx.shadowColor = `rgba(${activeColor[0]}, ${activeColor[1]}, ${activeColor[2]}, 0.5)`;
                } else {
                    ctx.shadowBlur = 0;
                }
                
                ctx.fill();
            }
        }
        
        function initCanvas() {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
            config = getParticleConfig();
            
            particles = [];
            for (let i = 0; i < config.count; i++) {
                particles.push(new Particle());
            }
        }
        
        // Draw links between close particles
        function connectParticles() {
            ctx.shadowBlur = 0; // Disable shadow for line drawing
            for (let i = 0; i < particles.length; i++) {
                for (let j = i + 1; j < particles.length; j++) {
                    const p1 = particles[i];
                    const p2 = particles[j];
                    
                    const dx = p1.x - p2.x;
                    const dy = p1.y - p2.y;
                    const dist = Math.sqrt(dx * dx + dy * dy);
                    
                    if (dist < config.maxLineDist) {
                        // Fade line opacity based on distance
                        const alpha = (1 - (dist / config.maxLineDist)) * 0.12;
                        ctx.beginPath();
                        ctx.moveTo(p1.x, p1.y);
                        ctx.lineTo(p2.x, p2.y);
                        ctx.strokeStyle = `rgba(${activeColor[0]}, ${activeColor[1]}, ${activeColor[2]}, ${alpha})`;
                        ctx.lineWidth = 0.8;
                        ctx.stroke();
                    }
                }
            }
        }
        
        function animate() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            particles.forEach(p => {
                p.update();
                p.draw();
            });
            
            connectParticles();
            
            animationFrameId = requestAnimationFrame(animate);
        }
        
        // Initialize and start loop
        initCanvas();
        animate();
        
        // Resize handling with debounce
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                cancelAnimationFrame(animationFrameId);
                initCanvas();
                animate();
            }, 200);
        });
    }
});
