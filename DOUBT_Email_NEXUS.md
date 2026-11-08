**Tiêu đề:** `DOUBT: NEXUS SERVICE MARKETING SYSTEM - Batch (điền) - Group (điền)`

Dear eProjects Team,

We are the team working on the Nexus Service Marketing System eProject (Le Tri Phuong, Lu Van Gioi, Nguyen Tran Xuan Cuong). We received the specification on 30/Oct/2026. Please confirm the points below. Our working assumption is given for each one, so a short "agree" is enough where it is correct.

**Schedule**

1. What is the official end date of the project? *Assumption: 30 days from 30/Oct/2026, so the end date is 28/Nov/2026.*

**Technology**

2. The specification lists old versions (Windows 98, j2sdk 1.4, SQL Server 2000 "or later"). May we use JDK 17, Tomcat 10.1, JSP/Servlets, JDBC and SQL Server 2019? *Assumption: yes.*

**Plans and prices**

3. The price of the STD Yearly plan is blank ("$"). What is the price? *Assumption: the Admin enters it; sample data uses our own value.*
4. The STD Half-Yearly plan says "valid for a month". Should this be 6 months? *Assumption: 6 months.*
5. Hourly plans have no rate for extra hours. What rate should apply? *Assumption: plan price divided by plan hours, configurable per plan.*
6. Is "Messaging For Mobiles" charged per minute or per message? *Assumption: per minute, as written.*

**Billing**

7. What exactly is the "advance"? *Assumption: the first-period plan fee after the bulk discount, which replaces the plan fee in the first bill.*
8. The 12.24% tax is "on the whole bill". Does it also apply to the refundable security deposit and to dues carried from earlier bills? *Assumption: no, because the deposit is refundable and earlier dues were already taxed. Example: Broadband 225$ + deposit 500$ gives tax 27.54$ and a total of 752.54$.*
9. Where do the call minutes and used hours come from? *Assumption: the Accounts department enters them when generating the bill.*
10. After how many unpaid days does a connection become temporarily inactive? *Assumption: 30 days; Accounts requests it and the Technical staff performs it.*
11. When a deposit is refunded, what is deducted? *Assumption: unpaid bills and replacement charges not yet billed; those amounts are then settled from the deposit, so the customer no longer owes them.*
12. Do the hours of an hourly plan apply to the whole validity period or to each month? *Assumption: the whole validity period, so "30 Hrs. valid for 3 Months" means 30 hours in total across the 3 months, and extra hours are counted cumulatively.*

**Orders and discounts**

13. The specification says that only retail employees place orders, but also that customers can place an order. Which is correct? *Assumption: customers send an online request, and a retail employee confirms it and creates the official order.*
14. Discount bands overlap (10-15, 15-25, 25-50, above 50). *Assumption: 10-15 = 25%, 16-25 = 50%, 26-50 = 75%, above 50 = 100%, below 10 = no discount, counted per order.*
15. For a new Dial-up with a new landline, is the deposit charged for both? *Assumption: yes, 325$ + 250$, as two connections with two account IDs.*
16. Is a "change plan" function required? The specification mentions customers trying to change their plan but does not list it as a requirement. *Assumption: not required unless you confirm.*

**Identifiers and roles**

17. The account ID is called "10 digit" in one place and "16 digit numeric" in another, yet its first character is a letter. *Assumption: 16 characters, 1 letter (D/B/T) + 3-digit city code + 12-digit serial.*
18. Equipment details are maintained "solely by the Admin" but also by "the technical people". *Assumption: the Admin manages the catalogue and purchases; the Technical staff issue, replace and adjust stock.*
19. How do customers log in? *Assumption: retail staff issue an account with a temporary password. Public search by ID shows only the status; name and due amount also require the registered phone number.*

Thank you.

Le Tri Phuong (Team Leader)
