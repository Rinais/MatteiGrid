-- =============================================================================
-- MatteiGrid Demo & Seed Data Script
-- Run this script in pgAdmin 4 Query Tool against `matteigrid_db`
-- =============================================================================

DO $$
DECLARE
    v_clinic_id UUID := '00000000-0000-0000-0000-000000000001';
    v_doctor_staff_id UUID := '11111111-1111-1111-1111-111111111111';
    v_doc_user_id UUID := '22222222-2222-2222-2222-222222222222';
    v_admin_user_id UUID := '33333333-3333-3333-3333-333333333333';
    v_reception_user_id UUID := '44444444-4444-4444-4444-444444444444';
    v_lab_user_id UUID := '55555555-5555-5555-5555-555555555555';
    v_superadmin_user_id UUID := '66666666-6666-6666-6666-666666666666';

    v_role_doctor UUID := '77777777-1111-0000-0000-000000000001';
    v_role_admin UUID := '77777777-2222-0000-0000-000000000002';
    v_role_reception UUID := '77777777-3333-0000-0000-000000000003';
    v_role_lab UUID := '77777777-4444-0000-0000-000000000004';
    v_role_superadmin UUID := '77777777-5555-0000-0000-000000000005';

    v_pat1_id UUID := 'aaaaaaaa-1111-0000-0000-000000000001';
    v_pat2_id UUID := 'aaaaaaaa-2222-0000-0000-000000000002';
    v_pat3_id UUID := 'aaaaaaaa-3333-0000-0000-000000000003';

    v_mm_s1 UUID := 'bbbbbbbb-0001-0000-0000-000000000001';
    v_mm_s2 UUID := 'bbbbbbbb-0002-0000-0000-000000000002';
    v_mm_a1 UUID := 'bbbbbbbb-0003-0000-0000-000000000003';
    v_mm_c1 UUID := 'bbbbbbbb-0004-0000-0000-000000000004';
    v_mm_p1 UUID := 'bbbbbbbb-0005-0000-0000-000000000005';
    v_mm_l1 UUID := 'bbbbbbbb-0006-0000-0000-000000000006';
    v_mm_ver1 UUID := 'bbbbbbbb-0007-0000-0000-000000000007';
    v_mm_f1 UUID := 'bbbbbbbb-0008-0000-0000-000000000008';

    v_med_s1 UUID := 'cccccccc-0001-0000-0000-000000000001';
    v_med_s2 UUID := 'cccccccc-0002-0000-0000-000000000002';
    v_med_a1 UUID := 'cccccccc-0003-0000-0000-000000000003';
    v_med_c1 UUID := 'cccccccc-0004-0000-0000-000000000004';
    v_med_p1 UUID := 'cccccccc-0005-0000-0000-000000000005';
    v_med_l1 UUID := 'cccccccc-0006-0000-0000-000000000006';
    v_med_ver1 UUID := 'cccccccc-0007-0000-0000-000000000007';
    v_med_f1 UUID := 'cccccccc-0008-0000-0000-000000000008';

    -- Password hash for 'Pass123!' standard ASP.NET Identity v3 hash
    v_pass_hash TEXT := 'AQAAAAIAAYagAAAAEPZf/nE3o2n2H1T2C7vM8yN4k9w1q7lK/b3q2p4z9l8x==';
BEGIN

    -- 1. CLINIC
    INSERT INTO public."Clinics" ("Id", "Name", "RazorpayLinkedAccountId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES (v_clinic_id, 'Mattei Grid Central Clinic', 'acc_matteigrid_demo', NOW(), NULL, NULL, NULL)
    ON CONFLICT ("Id") DO NOTHING;

    -- 2. ASP.NET IDENTITY ROLES
    INSERT INTO public."AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
    VALUES 
        (v_role_doctor, 'Doctor', 'DOCTOR', gen_random_uuid()::text),
        (v_role_admin, 'Administrator', 'ADMINISTRATOR', gen_random_uuid()::text),
        (v_role_reception, 'Receptionist', 'RECEPTIONIST', gen_random_uuid()::text),
        (v_role_lab, 'LabTechnician', 'LABTECHNICIAN', gen_random_uuid()::text),
        (v_role_superadmin, 'SuperAdmin', 'SUPERADMIN', gen_random_uuid()::text)
    ON CONFLICT ("Id") DO NOTHING;

    -- 3. ASP.NET IDENTITY USERS (Password: Pass123!)
    INSERT INTO public."AspNetUsers" ("Id", "ClinicId", "FullName", "IsActive", "TokenVersion", "CreatedAt", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber")
    VALUES
        (v_doc_user_id, v_clinic_id, 'Dr. Cesare Mattei', TRUE, 1, NOW(), 'doctor@clinic.com', 'DOCTOR@CLINIC.COM', 'doctor@clinic.com', 'DOCTOR@CLINIC.COM', TRUE, v_pass_hash, gen_random_uuid()::text, gen_random_uuid()::text, '+919876543200'),
        (v_admin_user_id, v_clinic_id, 'Clinic Administrator', TRUE, 1, NOW(), 'admin@clinic.com', 'ADMIN@CLINIC.COM', 'admin@clinic.com', 'ADMIN@CLINIC.COM', TRUE, v_pass_hash, gen_random_uuid()::text, gen_random_uuid()::text, '+919876543201'),
        (v_reception_user_id, v_clinic_id, 'Anita Sharma (Receptionist)', TRUE, 1, NOW(), 'reception@clinic.com', 'RECEPTION@CLINIC.COM', 'reception@clinic.com', 'RECEPTION@CLINIC.COM', TRUE, v_pass_hash, gen_random_uuid()::text, gen_random_uuid()::text, '+919876543202'),
        (v_lab_user_id, v_clinic_id, 'Rahul Verma (Lab Tech)', TRUE, 1, NOW(), 'lab@clinic.com', 'LAB@CLINIC.COM', 'lab@clinic.com', 'LAB@CLINIC.COM', TRUE, v_pass_hash, gen_random_uuid()::text, gen_random_uuid()::text, '+919876543203'),
        (v_superadmin_user_id, v_clinic_id, 'Platform SuperAdmin', TRUE, 1, NOW(), 'superadmin@matteigrid.com', 'SUPERADMIN@MATTEIGRID.COM', 'superadmin@matteigrid.com', 'SUPERADMIN@MATTEIGRID.COM', TRUE, v_pass_hash, gen_random_uuid()::text, gen_random_uuid()::text, '+919876543204')
    ON CONFLICT ("Id") DO NOTHING;

    -- 4. USER ROLES
    INSERT INTO public."AspNetUserRoles" ("UserId", "RoleId")
    VALUES
        (v_doc_user_id, v_role_doctor),
        (v_admin_user_id, v_role_admin),
        (v_reception_user_id, v_role_reception),
        (v_lab_user_id, v_role_lab),
        (v_superadmin_user_id, v_role_superadmin)
    ON CONFLICT DO NOTHING;

    -- 5. USER CLINIC MEMBERSHIPS
    INSERT INTO public.user_clinic_memberships ("Id", "UserId", "ClinicId", "ClinicName", "Role", "IsActive", "AssignedAt")
    VALUES
        (gen_random_uuid(), v_doc_user_id, v_clinic_id, 'Mattei Grid Central Clinic', 'Doctor', TRUE, NOW()),
        (gen_random_uuid(), v_admin_user_id, v_clinic_id, 'Mattei Grid Central Clinic', 'Administrator', TRUE, NOW()),
        (gen_random_uuid(), v_reception_user_id, v_clinic_id, 'Mattei Grid Central Clinic', 'Receptionist', TRUE, NOW()),
        (gen_random_uuid(), v_lab_user_id, v_clinic_id, 'Mattei Grid Central Clinic', 'LabTechnician', TRUE, NOW()),
        (gen_random_uuid(), v_superadmin_user_id, v_clinic_id, 'Mattei Grid Central Clinic', 'SuperAdmin', TRUE, NOW())
    ON CONFLICT ("UserId", "ClinicId") DO NOTHING;

    -- 6. STAFF
    INSERT INTO public."Staff" ("Id", "IdentityUserId", "Name", "Phone", "IsActive", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES (v_doctor_staff_id, v_doc_user_id, 'Dr. Cesare Mattei', '+919876543200', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("Id") DO NOTHING;

    -- 7. MATERIA MEDICA REMEDIES
    INSERT INTO public."MateriaMedicaEntries" ("Id", "Code", "Name", "Family", "Description", "Indications", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (v_mm_s1, 'S1', 'Scrofoloso 1', 'Scrofoloso', 'Constitutional remedy for lymphatic and glandular metabolism.', 'Lymphatic stasis, digestive sluggishness, scrofulous diathesis.', NOW(), NULL, NULL, NULL),
        (v_mm_s2, 'S2', 'Scrofoloso 2', 'Scrofoloso', 'Deep glandular tissue regenerator and metabolic balancer.', 'Chronic glandular swelling, scrofulous eczema.', NOW(), NULL, NULL, NULL),
        (v_mm_a1, 'A1', 'Angioitico 1', 'Angioitico', 'Cardiovascular and circulatory regulator. Affects venous and arterial tone.', 'Hypertension, hemorrhoids, venous stasis, palpitations.', NOW(), NULL, NULL, NULL),
        (v_mm_c1, 'C1', 'Canceroso 1', 'Canceroso', 'Degenerative cell protector and connective tissue restorative.', 'Fibroids, epithelial hyperplasia, cellular degeneration.', NOW(), NULL, NULL, NULL),
        (v_mm_p1, 'P1', 'Pettorale 1', 'Pettorale', 'Bronchopulmonary drainage remedy and lung restorative.', 'Bronchitis, chronic cough, respiratory catarrh.', NOW(), NULL, NULL, NULL),
        (v_mm_l1, 'L1', 'Linfatico 1', 'Linfatico', 'Universal fluid distributor and lymphatic cleanser.', 'Edema, lymphatic engorgement, lymphatic headaches.', NOW(), NULL, NULL, NULL),
        (v_mm_ver1, 'Ver1', 'Vermifugo 1', 'Vermifugo', 'Gastrointestinal flora harmonizer and antiparasitic.', 'Intestinal irritation, worm infestation symptoms.', NOW(), NULL, NULL, NULL),
        (v_mm_f1, 'F1', 'Febbrifugo 1', 'Febbrifugo', 'Thermoregulatory balancing agent for intermittent febrile states.', 'Fevers, periodic neuralgia, hepatic inflammation.', NOW(), NULL, NULL, NULL)
    ON CONFLICT ("Id") DO NOTHING;

    -- 8. DILUTIONS
    INSERT INTO public."Dilutions" ("Id", "Label", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (gen_random_uuid(), '1st Dilution (D1)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), '2nd Dilution (D2)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), '3rd Dilution (D3)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), '6C', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), '30C', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), '200C', v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("ClinicId", "Label") DO NOTHING;

    -- 9. VEHICLES
    INSERT INTO public."Vehicles" ("Id", "Name", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (gen_random_uuid(), 'Distilled Water (Aqua Destillata)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), 'Globules (Sugar of Milk)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), 'Diluted Alcohol (30%)', v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), 'Ointment Base', v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("ClinicId", "Name") DO NOTHING;

    -- 10. MEDICINES
    INSERT INTO public."Medicines" ("Id", "MateriaMedicaEntryId", "Name", "IsActive", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (v_med_s1, v_mm_s1, 'Scrofoloso 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_s2, v_mm_s2, 'Scrofoloso 2', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_a1, v_mm_a1, 'Angioitico 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_c1, v_mm_c1, 'Canceroso 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_p1, v_mm_p1, 'Pettorale 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_l1, v_mm_l1, 'Linfatico 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_ver1, v_mm_ver1, 'Vermifugo 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_med_f1, v_mm_f1, 'Febbrifugo 1', TRUE, v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("ClinicId", "Name") DO NOTHING;

    -- 11. INVENTORY ITEMS (Stock: 100 units, Reorder: 15)
    INSERT INTO public."InventoryItems" ("Id", "MedicineId", "QuantityOnHand", "ReorderLevel", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (gen_random_uuid(), v_med_s1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_s2, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_a1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_c1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_p1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_l1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_ver1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_med_f1, 100.00, 15.00, v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("ClinicId", "MedicineId") DO NOTHING;

    -- 12. DEMO PATIENTS
    INSERT INTO public."Patients" ("Id", "Name", "Phone", "DOB", "IsArchived", "IsAnonymized", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (v_pat1_id, 'Ramesh Kumar', '+919876543210', '1985-04-12', FALSE, FALSE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_pat2_id, 'Priya Sharma', '+919876543211', '1992-08-25', FALSE, FALSE, v_clinic_id, NOW(), NULL, NULL, NULL),
        (v_pat3_id, 'Suresh Patel', '+919876543212', '1978-11-03', FALSE, FALSE, v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("Id") DO NOTHING;

    -- 13. DEMO APPOINTMENTS
    INSERT INTO public."Appointments" ("Id", "PatientId", "DoctorStaffId", "StartAt", "EndAt", "Status", "OverrideReason", "ClinicId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
    VALUES
        (gen_random_uuid(), v_pat1_id, v_doctor_staff_id, NOW() + INTERVAL '1 hour', NOW() + INTERVAL '1 hour 30 min', 'Scheduled', NULL, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_pat2_id, v_doctor_staff_id, NOW() + INTERVAL '2 hours', NOW() + INTERVAL '2 hours 30 min', 'Scheduled', NULL, v_clinic_id, NOW(), NULL, NULL, NULL),
        (gen_random_uuid(), v_pat3_id, v_doctor_staff_id, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day' + INTERVAL '30 min', 'Completed', NULL, v_clinic_id, NOW(), NULL, NULL, NULL)
    ON CONFLICT ("Id") DO NOTHING;

END $$;
