# Narration LLM Audit

LLM-rewritten narrations for scenes whose OCR diverges from the intended text.

| Pad | Jaccard | LLM Score | Original | Suggested Rewrite |
|-----|---------|-----------|----------|-------------------|
| 001 | 0.04 | ? | Wolfs moves cars across the world. |  |
| 002 | 0.00 | ? | Seller signs in with Google. |  |
| 003 | 0.04 | ? | Seller taps Sell on the home page. |  |
| 004 | 0.06 | ? | Seller starts a chat with the agent. |  |
| 005 | 0.06 | ? | Agent asks the car and pickup factory. |  |
| 006 | 0.02 | ? | Agent asks the cash to bring. |  |
| 007 | 0.04 | ? | Agent asks the buyer and destination. |  |
| 008 | 0.05 | ? | Agent asks the price and payment method. |  |
| 009 | 0.04 | ? | Agent writes the job; seller publishes. |  |
| 010 | 0.08 | ? | Agent saves the listing and returns the link. |  |
| 011 | 0.09 | ? | BYD Han EV listing renders live in marketplace. |  |
| 012 | 0.10 | ? | Driver from China signs in with Okta. |  |
| 013 | 0.05 | ? | Driver from China taps Apply. |  |
| 014 | 0.10 | ? | Driver chats; agent asks name and years driving. |  |
| 015 | 0.08 | ? | Agent requests CDL and China export pass. |  |
| 016 | 0.04 | ? | Driver uploads CDL and export pass. |  |
| 017 | 0.05 | ? | Agent checks the license and confirms it expires in 2030. |  |
| 018 | 0.05 | ? | Driver from China sees pending admin approval. |  |
| 019 | 0.00 | ? | Driver from LA signs in with Google. |  |
| 020 | 0.04 | ? | Driver from LA taps Apply. |  |
| 021 | 0.04 | ? | Driver from LA chats; shares his details. |  |
| 022 | 0.07 | ? | Agent requests TWIC port pass and drayage card. |  |
| 023 | 0.02 | ? | Driver from LA uploads license, port pass, drayage card. |  |
| 024 | 0.03 | ? | Driver from LA sees pending admin approval. |  |
| 025 | 0.04 | ? | Team driver in Phoenix signs in with Microsoft. |  |
| 026 | 0.05 | ? | Team driver taps Apply. |  |
| 027 | 0.04 | ? | Team driver chats; shares his details. |  |
| 028 | 0.04 | ? | Agent requests team-driver papers. |  |
| 029 | 0.02 | ? | Team driver uploads papers and both licenses. |  |
| 030 | 0.05 | ? | Team driver sees pending admin approval. |  |
| 031 | 0.00 | ? | Driver in Wilmington signs in with Google. |  |
| 032 | 0.04 | ? | Driver in Wilmington taps Apply. |  |
| 033 | 0.04 | ? | Driver in Wilmington chats; shares his details. |  |
| 034 | 0.05 | ? | Agent requests auto-handling cert. |  |
| 035 | 0.02 | ? | Driver in Wilmington uploads license and cert. |  |
| 036 | 0.03 | ? | Driver in Wilmington sees pending admin approval. |  |
| 037 | 0.00 | ? | Admin signs in with GitHub. |  |
| 038 | 0.11 | ? | Admin home shows four pending applicants. |  |
| 039 | 0.07 | ? | Admin opens the hiring hall list. |  |
| 040 | 0.14 | ? | Admin clicks Approve all and assigns badges. |  |
| 041 | 0.03 | ? | All four drivers are hired. |  |
| 042 | 0.04 | ? | Driver from China sees he is hired. |  |
| 043 | 0.06 | ? | Driver from China lands on driver home. |  |
| 044 | 0.02 | ? | Driver from LA sees he is hired. |  |
| 045 | 0.06 | ? | Driver from LA lands on driver home. |  |
| 046 | 0.04 | ? | Team driver sees he is hired. |  |
| 047 | 0.06 | ? | Team driver lands on driver home. |  |
| 048 | 0.02 | ? | Driver in Wilmington sees he is hired. |  |
| 049 | 0.06 | ? | Driver in Wilmington lands on driver home. |  |
| 050 | 0.06 | ? | Buyer lands on the Wolfs home page. |  |
| 051 | 0.04 | ? | Buyer signs in with Microsoft. |  |
| 052 | 0.14 | ? | Buyer sees the BYD listing in marketplace. |  |
| 053 | 0.06 | ? | Buyer enters shipping address. |  |
| 054 | 0.06 | ? | Buyer enters delivery contact. |  |
| 055 | 0.10 | ? | Buyer picks delivery day and time. |  |
| 056 | 0.06 | ? | Buyer picks pay on delivery. |  |
| 057 | 0.04 | ? | Buyer adds notes and confirms order. |  |
| 058 | 0.00 | ? | Driver from China starts the map. |  |
| 059 | 0.00 | ? | Head west to the highway. |  |
| 060 | 0.00 | ? | Take the exit toward Hefei. |  |
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
