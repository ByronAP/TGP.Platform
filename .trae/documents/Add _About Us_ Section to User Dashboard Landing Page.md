# Create "About Us" Page with 3 Team Members

## 1. Create New Page: `Pages/About.cshtml`
Create a new Razor Page `TGP.UserDashboard/src/TGP.UserDashboard/Pages/About.cshtml` and its backing model.
- **Access**: Public (Anonymous)
- **Layout**: Uses `_LandingLayout` for guests.
- **Content**:
  - **Hero Section**: "About Us"
  - **Mission Statement**: Focus on transparency and family safety.
  - **Meet the Team Section**: A grid containing **3 Team Member Cards**:
    1.  **Allen [Last Name]** - Founder & CEO
        - Bio mentioning "TGP Controls".
        - LinkedIn button (placeholder).
    2.  **[Team Member Name]** - [Role]
        - Bio placeholder.
        - LinkedIn button (placeholder).
    3.  **[Team Member Name]** - [Role]
        - Bio placeholder.
        - LinkedIn button (placeholder).
    - *Note: All cards will use placeholder images (SVG avatars) until updated with real photos.*

## 2. Update Navigation
- **`Pages/Shared/_LandingLayout.cshtml`**: Add "About Us" link to the main navigation bar and mobile menu.

## 3. Update Footer
- **`Pages/Index.cshtml`**: Add "About Us" link to the Footer section.

## 4. Verification
- Verify the page displays 3 team member profiles correctly.
- Verify anonymous access.
