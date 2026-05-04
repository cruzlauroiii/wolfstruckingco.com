# Narration LLM Audit

LLM-rewritten narrations for scenes whose OCR diverges from the intended text.

| Pad | Jaccard | LLM Score | Original | Suggested Rewrite |
|-----|---------|-----------|----------|-------------------|
| 001 | 0.04 | 0.60 | Wolfs moves cars across the world. | Wolfs Trucking Co. moves anything, anywhere on demand. Whether it's a car, a pallet, a container, or a single crate, tell our agent what you need shipped and we'll handle the logistics. |
| 002 | 0.00 | 0.10 | Seller signs in with Google. | The Wolfs Trucking home page displays options to access the Marketplace, Apply to drive, or Log off. The page describes Wolfs Trucking Co. as a logistics platform where you tell an agent what you need... |
| 003 | 0.04 | 0.30 | Seller taps Sell on the home page. | User views the Wolfs Trucking Co. home page, which displays options to tell the agent what to ship, access the marketplace, or apply to drive. |
| 004 | 0.06 | 0.80 | Seller starts a chat with the agent. | The shipper Wei Zhang initiates a chat with the Wolfs agent to post a shipping job for a 2024 BYD Han EV from China to the US. The agent confirms the shipment details and asks for pickup date, US dest... |
| 005 | 0.06 | 0.60 | Agent asks the car and pickup factory. | Agent helps the customer post a China-export shipment. The customer tells the agent they're shipping a BYD Han EV from a factory in Hefei, China to Wilmington, North Carolina on June 15th. The agent c... |
| 006 | 0.02 | 0.10 | Agent asks the cash to bring. | Agent confirms the shipment details and asks the customer whether they need a port of entry assigned or if the carrier should coordinate that when accepting the job. |
| 007 | 0.04 | 0.30 | Agent asks the buyer and destination. | Agent confirms the buyer details—Sam Chen from Wilmington, North Carolina—and the destination is locked in with a 45-day delivery window. Payment terms and shipment specifics like the factory invoice ... |
| 008 | 0.05 | 0.80 | Agent asks the price and payment method. | Agent confirms the sale price of $48,500 with cash on delivery via real-time payment at delivery. The listing is fully updated with all route badges active and ready for carrier bids. |
| 009 | 0.04 | 0.80 | Agent writes the job; seller publishes. | Customer tells the agent what they're shipping. The agent writes up the job posting with all the details—price, payment terms, pickup and delivery locations, and the right route badges. Once everythin... |
| 010 | 0.08 | 0.80 | Agent saves the listing and returns the link. | Agent confirms the listing has been saved. The system provides the marketplace link so carriers can view and apply for the job. The listing is now live and ready to receive applications. |
| 011 | 0.09 | 0.15 | BYD Han EV listing renders live in marketplace. | The marketplace is currently empty with no listings available. A message invites users to be the first to post a listing. |
| 012 | 0.10 | 0.85 | Driver from China signs in with Okta. | The driver enters their email address and signs in using their account credentials. |
| 013 | 0.05 | 0.80 | Driver from China taps Apply. | A driver interested in joining Wolfs can tap the Start Application button to chat with our agent, who will walk them through what documents they'll need—like their driver's license, medical card, and ... |
| 014 | 0.10 | 0.85 | Driver chats; agent asks name and years driving. | Driver Wei Liu introduces himself and shares his background. Agent confirms the information and asks about certifications and preferred driving regions. |
| 015 | 0.08 | 0.90 | Agent requests CDL and China export pass. | Agent asks Wei for his certifications and preferred driving regions. Wei provides his CDL and China export pass. Agent confirms both documents are saved to his application, then asks about availabilit... |
| 016 | 0.04 | 0.60 | Driver uploads CDL and export pass. | Driver uploads their CDL and other required certificates like the DOT medical card and any endorsements for hazmat, tanker, or doubles and triples. |
| 017 | 0.05 | 0.15 | Agent checks the license and confirms it expires in 2030. | Agent greets you in the chat and asks what you're shipping so he can write up a job posting for you. You can also use the marketplace to apply to drive jobs, or log off from your account. |
| 018 | 0.05 | 0.50 | Driver from China sees pending admin approval. | Driver from China can apply to drive for Wolfs. You'll chat with our agent who'll ask for your license, medical card, port documents, and China export pass. Once the agent verifies everything, they'll... |
| 019 | 0.00 | 0.30 | Driver from LA signs in with Google. | A user logged in as cruzlauroiii can access the Wolfs Trucking home page, which offers options to use the Marketplace, Apply to drive, or Log off. The platform describes itself as a logistics service ... |
| 020 | 0.04 | 0.85 | Driver from LA taps Apply. | Driver from LA views the application page showing what documents are needed—license, medical card, port passes, and other certifications—then taps Start application to chat with an agent |
| 021 | 0.04 | 0.65 | Driver from LA chats; shares his details. | Marco Rivera, a driver from San Pedro, California, logs into the Wolfs platform and chats with an agent. He shares his background in Port of LA drayage work and his CDL qualifications. The agent confi... |
| 022 | 0.07 | 0.65 | Agent requests TWIC port pass and drayage card. | Agent confirms Marco's TWIC and CDL-A credentials are saved, adds him to the BYD Han EV run, and asks what he's shipping next. |
| 023 | 0.02 | 0.65 | Driver from LA uploads license, port pass, drayage card. | Driver from LA accesses their documents portal and uploads certificates matching their badges. They can add or replace documents by tapping each card—CDL Class A front and back for tractor-trailers, C... |
| 024 | 0.03 | 0.65 | Driver from LA sees pending admin approval. | Driver from LA visits the application page to become a Wolfs driver. The page explains what documents you'll need—your driver's license, medical card, any port or drayage cards, auto-handling cert, te... |
| 025 | 0.04 | 0.65 | Team driver in Phoenix signs in with Microsoft. | Team driver in Phoenix signs in with their Microsoft account. The system recognizes an existing account for Lauro Ill Cruz and prompts them to enter their email, phone, or Skype to continue. |
| 026 | 0.05 | 0.85 | Team driver taps Apply. | A team driver reviews what they need to apply, including their license, medical card, any port or drayage credentials, and team-driver papers if working with a partner. They then start the application... |
| 027 | 0.04 | 0.80 | Team driver chats; shares his details. | Team drivers Diego Morales and Maria Santos from Phoenix are applying to drive with Wolfs. They've been saved to our system, and the agent is now asking about their CDL certifications and what they'll... |
| 028 | 0.04 | 0.80 | Agent requests team-driver papers. | Agent confirms the team driver application is saved and asks about CDL certifications and any special requirements like Hazmat or Tanker endorsements for Diego and Maria's profile. |
| 029 | 0.02 | 0.85 | Team driver uploads papers and both licenses. | Team driver uploads their CDL license and DOT medical card to complete their required documents in the My Documents section. |
| 030 | 0.05 | 0.65 | Team driver sees pending admin approval. | Team driver application page. You'll need your driver's license, medical card, any port or drayage cards you have, auto-handling cert if you do final-mile delivery, team-driver papers if you have a pa... |
| 031 | 0.00 | 0.20 | Driver in Wilmington signs in with Google. | User logged in as cruzlauroiii is viewing the Wolfs Trucking home page, which describes the platform as a logistics service for shipping various cargo types with dynamic driver and credential assignme... |
| 032 | 0.04 | 0.80 | Driver in Wilmington taps Apply. | Driver in Wilmington reviews the application requirements—license, medical card, and any special credentials—then taps Start application to chat with our agent. |
| 033 | 0.04 | 0.85 | Driver in Wilmington chats; shares his details. | Sam Chen Jr from Wilmington, North Carolina introduces himself as a driver with four years of auto-handling experience. The agent acknowledges his profile has been saved and asks if he has any CDL end... |
| 034 | 0.05 | 0.30 | Agent requests auto-handling cert. | Agent confirms driver's qualifications and gets him registered. Sam provides his CDL-A status and current inspection cert. Agent confirms all details are saved and asks what he's looking to haul. |
| 035 | 0.02 | 0.80 | Driver in Wilmington uploads license and cert. | Driver in Wilmington is on the My Documents page uploading their CDL, medical card, and endorsements. The system shows required documents like Class A or B license, DOT medical certificate, and option... |
| 036 | 0.03 | 0.25 | Driver in Wilmington sees pending admin approval. | Driver visits the Wolfs application page to learn what documents are needed: driver's license front and back, medical card, any port pass or drayage card, auto-handling cert for final-mile work, team-... |
| 037 | 0.00 | 0.30 | Admin signs in with GitHub. | The Wolfs Trucking home page is displayed, showing the user is logged in as cruzlauroiii. The page features navigation options for Marketplace and Apply to drive, with a tagline describing the service... |
| 038 | 0.11 | 0.80 | Admin home shows four pending applicants. | Admin home displays pending applicants, an on-time rate of 100%, active jobs, and net revenue of 46 thousand dollars. |
| 039 | 0.07 | 0.80 | Admin opens the hiring hall list. | Admin opens the hiring hall to review and approve new drivers. The system shows all applicants waiting for approval, with options to auto-approve everyone and assign badges and roles in one batch, or ... |
| 040 | 0.14 | 0.85 | Admin clicks Approve all and assigns badges. | Admin clicks Approve all to automatically assign badges and roles to all applicants at once. |
| 041 | 0.03 | 0.30 | All four drivers are hired. | You're in the Hiring Hall with all applicants ready. You can approve them together in one batch to assign badges and roles at once, or review each one individually. |
| 042 | 0.04 | 0.20 | Driver from China sees he is hired. | To drive for Wolfs, you'll need to gather your documents: driver's license front and back, medical card, any port pass or drayage card, auto-handling cert if you do final-mile work, team-driver papers... |
| 043 | 0.06 | 0.30 | Driver from China lands on driver home. | Driver lands on the home screen showing their next job, earnings, and turn-by-turn directions. This week shows zero dollars. Quick links include Marketplace, Schedule, Call agent, Apply to drive, and ... |
| 044 | 0.02 | 0.30 | Driver from LA sees he is hired. | Driver from LA visits the application page to see what documents and certifications are needed to drive for Wolfs, including license, medical card, and any specialty credentials like port passes or au... |
| 045 | 0.06 | 0.85 | Driver from LA lands on driver home. | Driver logs in to the home screen. They can see their weekly earnings of zero dollars, quick links including Marketplace, and a message that no job offers are currently available. The screen shows sev... |
| 046 | 0.04 | 0.20 | Team driver sees he is hired. | To drive for Wolfs, you'll need to gather your documents—your driver's license front and back, medical card, and any special certifications or passes you have like port passes, drayage cards, intersta... |
| 047 | 0.06 | 0.80 | Team driver lands on driver home. | Team driver lands on the driver home screen showing earnings, quick links to the marketplace and schedule, and a message that no job offers are currently available. |
| 048 | 0.02 | 0.20 | Driver in Wilmington sees he is hired. | Driver arrives at Wolfs' home page and sees the application process. The page explains what documents you'll need to apply—your driver's license, medical card, any port passes or drayage cards, auto-h... |
| 049 | 0.06 | 0.85 | Driver in Wilmington lands on driver home. | Driver lands on the home screen showing their next job opportunities, earnings summary, and available quick links like the marketplace and schedule. |
| 050 | 0.06 | 0.90 | Buyer lands on the Wolfs home page. | Buyer lands on the Wolfs Trucking home page and sees the main value proposition: a logistics platform that ships anything anywhere on demand, from cars to pallets to crates, with dynamic routing and d... |
| 051 | 0.04 | 0.85 | Buyer signs in with Microsoft. | A buyer enters their email, phone, or Skype to sign in with Microsoft. The system recognizes an existing account for Lauro Cruz and offers sign-in options. |
| 052 | 0.14 | 0.20 | Buyer sees the BYD listing in marketplace. | Buyer navigates to the Marketplace but finds no listings available yet. The page shows an empty state with a message inviting users to be the first to post a listing. |
| 053 | 0.06 | 0.80 | Buyer enters shipping address. | Buyer enters their shipping address details including name, street address, city, state, and postal code. |
| 054 | 0.06 | 0.85 | Buyer enters delivery contact. | Buyer provides their phone number and a backup contact for delivery, so the driver knows how to reach them at the door. |
| 055 | 0.10 | 0.85 | Buyer picks delivery day and time. | Buyer selects their preferred delivery window by choosing the earliest and latest dates, then picks a time slot between 9 AM and 10 PM. |
| 056 | 0.06 | 0.85 | Buyer picks pay on delivery. | The buyer selects pay on delivery as their payment method. The order total of $48,500 will be due at the door, and a receipt will be sent to sam@buyers.example. |
| 057 | 0.04 | 0.85 | Buyer adds notes and confirms order. | Buyer reviews special delivery instructions and confirms the order for $48,500 due on delivery. |
| 058 | 0.00 | 0.20 | Driver from China starts the map. | Driver is on the Wolfs home screen with no active navigation. Options visible include Marketplace, Apply to drive, and Log off. |
| 059 | 0.00 | 0.10 | Head west to the highway. | You're on the Wolfs Trucking home page. There's no active navigation right now. You can access the marketplace, apply to drive, or log off from here. |
| 060 | 0.00 | 0.00 | Take the exit toward Hefei. | You're on the Wolfs Trucking home page with no active navigation. You can access the Marketplace, Apply to drive, or Log off from here. |
| 061 | 0.00 | ? | Continue three hundred kilometers. |  |
| 062 | 0.00 | ? | Arrive at the BYD factory. |  |
| 063 | 0.09 | ? | Driver tells agent he is at the factory. |  |
| 064 | 0.05 | ? | Agent confirms factory cash payment. |  |
| 065 | 0.05 | ? | Driver places GPS tracker in the car. |  |
| 066 | 0.00 | ? | Head east to Shanghai port. |  |
| 067 | 0.00 | ? | Take the bridge to terminal four. |  |
| 068 | 0.00 | ? | Arrive at the port. |  |
| 069 | 0.02 | ? | Driver loads car into ship container. |  |
| 070 | 0.00 | ? | Ship leaves Shanghai. |  |
| 071 | 0.02 | ? | Buyer watches the ship cross the ocean. |  |
| 072 | 0.00 | ? | Ship is halfway to Los Angeles. |  |
| 073 | 0.00 | ? | Ship arrives at Los Angeles. |  |
| 074 | 0.08 | ? | Agent tells LA driver the ship is here. |  |
| 075 | 0.00 | ? | LA driver starts the map. |  |
| 076 | 0.00 | ? | Head north to the port. |  |
| 077 | 0.00 | ? | Show port pass at gate B. |  |
| 078 | 0.00 | ? | Pick up car from the yard. |  |
| 079 | 0.08 | ? | LA driver tells agent he picked up the car. |  |
| 080 | 0.00 | ? | Head east on highway to Phoenix. |  |
| 081 | 0.02 | ? | Traffic slows I-10, so the arrival time updates. |  |
| 082 | 0.08 | ? | Dispatcher sees the new ETA on the live board. |  |
| 083 | 0.11 | ? | Agent tells buyer the new ETA. |  |
| 084 | 0.04 | ? | Schedule updates the next drivers. |  |
| 085 | 0.00 | ? | Arrive at Phoenix. |  |
| 086 | 0.04 | ? | LA driver finishes his leg. |  |
| 087 | 0.06 | ? | Agent tells team driver to start. |  |
| 088 | 0.00 | ? | Team driver starts the map. |  |
| 089 | 0.00 | ? | Head east on the highway. |  |
| 090 | 0.00 | ? | Continue east on I-40 through Albuquerque. |  |
| 091 | 0.00 | ? | Continue to Memphis. |  |
| 092 | 0.00 | ? | Arrive at the Memphis yard. |  |
| 093 | 0.07 | ? | Team driver finishes the leg. |  |
| 094 | 0.09 | ? | Agent tells Wilmington driver to start last leg. |  |
| 095 | 0.00 | ? | Wilmington driver starts the map. |  |
| 096 | 0.00 | ? | Head east on the highway. |  |
| 097 | 0.00 | ? | Turn south to Wilmington. |  |
| 098 | 0.00 | ? | Turn onto Oak Street. |  |
| 099 | 0.00 | ? | Arrive at fourteen-eighteen Oak Street. |  |
| 100 | 0.07 | ? | Wilmington driver calls buyer from the door. |  |
| 101 | 0.07 | ? | Buyer comes to the door. |  |
| 102 | 0.05 | ? | Buyer inspects the car. |  |
| 103 | 0.04 | ? | Buyer pays at the door. |  |
| 104 | 0.04 | ? | Driver takes a delivery photo. |  |
| 105 | 0.06 | ? | Driver hands over the keys. |  |
| 106 | 0.00 | ? | Admin opens the business dashboard. |  |
| 107 | 0.03 | ? | All four drivers paid for their legs. |  |
| 108 | 0.03 | ? | Factory cash and shipping reimbursed. |  |
| 109 | 0.03 | ? | Customs fees and buyer settlement cleared. |  |
| 110 | 0.03 | ? | Wolfs records its service fee. |  |
| 111 | 0.00 | ? | The money totals match exactly. |  |
| 112 | 0.03 | ? | Every delivery was on time. |  |
| 113 | 0.00 | ? | Every payment cleared. |  |
| 114 | 0.11 | ? | The listing is closed. |  |
| 115 | 0.00 | ? | The order is delivered. |  |
| 116 | 0.03 | ? | Driver from China rates the trip. |  |
| 117 | 0.02 | ? | Buyer rates the delivery. |  |
| 118 | 0.00 | ? | Seller sees the sale on his dashboard. |  |
| 119 | 0.03 | ? | Admin closes the day on the ops board. |  |
| 120 | 0.06 | ? | Wolfs moves the next car. |  |
| 121 | 0.04 | ? | End title — Wolfs Trucking Co. |  |

_Reviewed 121 scenes, 0 errors. Threshold = 0.40._
